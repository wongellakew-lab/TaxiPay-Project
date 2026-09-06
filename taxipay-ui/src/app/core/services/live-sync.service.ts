import { Injectable, effect, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { ActivePassengerDto } from '../models/driver.models';
import { PassengerRouteRequestDto } from '../models/passenger.models';
import { AdminDispute, AdminRouteChangeRequest } from '../models/admin.models';
import { AppNotificationDto } from '../models/notification.models';
import {
  DriverStatusUpdateDto,
  PassengerRouteRequestNotificationDto,
  RouteChangeNotificationDto,
  TripStatusUpdateDto
} from '../models/realtime.models';

@Injectable({
  providedIn: 'root'
})
export class LiveSyncService {
  private auth = inject(AuthService);
  private hubConnection: signalR.HubConnection | null = null;

  // Connection State Signal
  public isConnected = signal<boolean>(false);
  public connectionError = signal<string | null>(null);

  // Event Subjects
  private passengerAddedToManifestSubject = new Subject<ActivePassengerDto>();
  private alightingRequestedSubject = new Subject<ActivePassengerDto>();
  private alightingConfirmedSubject = new Subject<ActivePassengerDto>();
  private tripStatusUpdatedSubject = new Subject<TripStatusUpdateDto>();
  private driverStatusUpdatedSubject = new Subject<DriverStatusUpdateDto>();
  private driverRouteChangeSubmittedSubject = new Subject<AdminRouteChangeRequest>();
  private routeChangeReviewedSubject = new Subject<RouteChangeNotificationDto>();
  private passengerRouteRequestSubmittedSubject = new Subject<PassengerRouteRequestDto>();
  private passengerRouteRequestReviewedSubject = new Subject<PassengerRouteRequestNotificationDto>();
  private disputeUpdatedSubject = new Subject<AdminDispute>();
  private notificationCreatedSubject = new Subject<AppNotificationDto>();
  private reconnectedSubject = new Subject<void>();

  // Public Observables
  public readonly passengerAddedToManifest$: Observable<ActivePassengerDto> =
    this.passengerAddedToManifestSubject.asObservable();
  public readonly alightingRequested$: Observable<ActivePassengerDto> =
    this.alightingRequestedSubject.asObservable();
  public readonly alightingConfirmed$: Observable<ActivePassengerDto> =
    this.alightingConfirmedSubject.asObservable();
  public readonly tripStatusUpdated$: Observable<TripStatusUpdateDto> =
    this.tripStatusUpdatedSubject.asObservable();
  public readonly driverStatusUpdated$: Observable<DriverStatusUpdateDto> =
    this.driverStatusUpdatedSubject.asObservable();
  public readonly driverRouteChangeSubmitted$: Observable<AdminRouteChangeRequest> =
    this.driverRouteChangeSubmittedSubject.asObservable();
  public readonly routeChangeReviewed$: Observable<RouteChangeNotificationDto> =
    this.routeChangeReviewedSubject.asObservable();
  public readonly passengerRouteRequestSubmitted$: Observable<PassengerRouteRequestDto> =
    this.passengerRouteRequestSubmittedSubject.asObservable();
  public readonly passengerRouteRequestReviewed$: Observable<PassengerRouteRequestNotificationDto> =
    this.passengerRouteRequestReviewedSubject.asObservable();
  public readonly disputeUpdated$: Observable<AdminDispute> =
    this.disputeUpdatedSubject.asObservable();
  public readonly notificationCreated$: Observable<AppNotificationDto> =
    this.notificationCreatedSubject.asObservable();
  public readonly reconnected$: Observable<void> =
    this.reconnectedSubject.asObservable();

  constructor() {
    // Automatically manage connection lifecycle based on authentication state
    effect(() => {
      const user = this.auth.currentUser();
      if (user && user.token) {
        this.startConnection();
      } else {
        this.stopConnection();
      }
    });
  }

  public async startConnection(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connecting) {
      return;
    }

    const hubUrl = (environment as any).hubUrl || `${environment.apiUrl.replace(/\/api$/, '')}/hubs/taxipay`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.auth.currentUser()?.token || '',
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.registerEventHandlers(this.hubConnection);

    try {
      await this.hubConnection.start();
      this.isConnected.set(true);
      this.connectionError.set(null);
      console.log('⚡ TaxiPay LiveSync SignalR connected successfully.');

      // Enroll in role/user groups upon connect
      this.joinUserRoleGroups();
    } catch (err: any) {
      this.isConnected.set(false);
      this.connectionError.set(err?.message || 'Failed to connect to real-time hub');
      console.warn('⚠️ TaxiPay LiveSync connection attempt failed:', err);
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
      } catch (err) {
        console.warn('Error stopping SignalR connection:', err);
      } finally {
        this.hubConnection = null;
        this.isConnected.set(false);
      }
    }
  }

  private registerEventHandlers(connection: signalR.HubConnection): void {
    // Reconnection Lifecycle
    connection.onreconnecting(err => {
      this.isConnected.set(false);
      console.warn('🔄 TaxiPay LiveSync reconnecting...', err);
    });

    connection.onreconnected(connectionId => {
      this.isConnected.set(true);
      console.log('✅ TaxiPay LiveSync reconnected. ConnectionId:', connectionId);
      this.joinUserRoleGroups();
      this.reconnectedSubject.next();
    });

    connection.onclose(err => {
      this.isConnected.set(false);
      console.warn('🔌 TaxiPay LiveSync connection closed.', err);
    });

    // Strongly-Typed Client Event Handlers matching ITaxiPayHubClient
    connection.on('ReceivePassengerAddedToManifest', (passenger: ActivePassengerDto) => {
      console.log('🔔 [SignalR] ReceivePassengerAddedToManifest:', passenger);
      this.passengerAddedToManifestSubject.next(passenger);
    });

    connection.on('ReceiveAlightingRequested', (passenger: ActivePassengerDto) => {
      console.log('🔔 [SignalR] ReceiveAlightingRequested:', passenger);
      this.alightingRequestedSubject.next(passenger);
    });

    connection.on('ReceiveAlightingConfirmed', (passenger: ActivePassengerDto) => {
      console.log('🔔 [SignalR] ReceiveAlightingConfirmed:', passenger);
      this.alightingConfirmedSubject.next(passenger);
    });

    connection.on('ReceiveTripStatusUpdated', (status: TripStatusUpdateDto) => {
      console.log('🔔 [SignalR] ReceiveTripStatusUpdated:', status);
      this.tripStatusUpdatedSubject.next(status);
    });

    connection.on('ReceiveDriverStatusUpdated', (status: DriverStatusUpdateDto) => {
      console.log('🔔 [SignalR] ReceiveDriverStatusUpdated:', status);
      this.driverStatusUpdatedSubject.next(status);
    });

    connection.on('ReceiveDriverRouteChangeSubmitted', (request: AdminRouteChangeRequest) => {
      console.log('🔔 [SignalR] ReceiveDriverRouteChangeSubmitted:', request);
      this.driverRouteChangeSubmittedSubject.next(request);
    });

    connection.on('ReceiveRouteChangeReviewed', (update: RouteChangeNotificationDto) => {
      console.log('🔔 [SignalR] ReceiveRouteChangeReviewed:', update);
      this.routeChangeReviewedSubject.next(update);
    });

    connection.on('ReceivePassengerRouteRequestSubmitted', (request: PassengerRouteRequestDto) => {
      console.log('🔔 [SignalR] ReceivePassengerRouteRequestSubmitted:', request);
      this.passengerRouteRequestSubmittedSubject.next(request);
    });

    connection.on('ReceivePassengerRouteRequestReviewed', (request: PassengerRouteRequestNotificationDto) => {
      console.log('🔔 [SignalR] ReceivePassengerRouteRequestReviewed:', request);
      this.passengerRouteRequestReviewedSubject.next(request);
    });

    connection.on('ReceiveDisputeUpdated', (dispute: AdminDispute) => {
      console.log('🔔 [SignalR] ReceiveDisputeUpdated:', dispute);
      this.disputeUpdatedSubject.next(dispute);
    });

    connection.on('ReceiveNotificationCreated', (notification: AppNotificationDto) => {
      console.log('🔔 [SignalR] ReceiveNotificationCreated:', notification);
      this.notificationCreatedSubject.next(notification);
    });
  }

  private joinUserRoleGroups(): void {
    const user = this.auth.currentUser();
    if (!user) return;

    if (user.role === 'driver') {
      const did = user.driverCode || user.driverData?.driverId;
      if (did) {
        this.joinDriverGroup(did);
      }
      if (user.id) {
        this.joinDriverGroup(user.id);
      }
    } else if (user.role === 'passenger' && user.id) {
      this.joinPassengerGroup(user.id);
    } else if (user.role === 'admin') {
      this.joinAdminGroup();
    }
  }

  // Explicit Group Join / Leave Methods
  public async joinDriverGroup(driverId: string): Promise<void> {
    if (!driverId) return;
    try {
      if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
        await this.hubConnection.invoke('JoinDriverGroup', driverId);
      }
    } catch (err) {
      console.warn(`Could not join driver group for ${driverId}:`, err);
    }
  }

  public async leaveDriverGroup(driverId: string): Promise<void> {
    if (!driverId) return;
    try {
      if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
        await this.hubConnection.invoke('LeaveDriverGroup', driverId);
      }
    } catch (err) {
      console.warn(`Could not leave driver group for ${driverId}:`, err);
    }
  }

  public async joinPassengerGroup(passengerId: string): Promise<void> {
    if (!passengerId) return;
    try {
      if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
        await this.hubConnection.invoke('JoinPassengerGroup', passengerId);
      }
    } catch (err) {
      console.warn(`Could not join passenger group for ${passengerId}:`, err);
    }
  }

  public async leavePassengerGroup(passengerId: string): Promise<void> {
    if (!passengerId) return;
    try {
      if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
        await this.hubConnection.invoke('LeavePassengerGroup', passengerId);
      }
    } catch (err) {
      console.warn(`Could not leave passenger group for ${passengerId}:`, err);
    }
  }

  public async joinTripGroup(tripId: string): Promise<void> {
    if (!tripId) return;
    try {
      if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
        await this.hubConnection.invoke('JoinTripGroup', tripId);
      }
    } catch (err) {
      console.warn(`Could not join trip group for ${tripId}:`, err);
    }
  }

  public async joinAdminGroup(): Promise<void> {
    try {
      if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
        await this.hubConnection.invoke('JoinAdminGroup');
      }
    } catch (err) {
      console.warn('Could not join admin group:', err);
    }
  }
}
