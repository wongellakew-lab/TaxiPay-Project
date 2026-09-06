import {
  Component,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
  MatAutocompleteTrigger
} from '@angular/material/autocomplete';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, Subscription, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { PassengerService } from '../../../core/services/passenger.service';
import { LiveSyncService } from '../../../core/services/live-sync.service';
import { WalletStore } from '../../../core/stores/wallet.store';
import { CurrentJourneyStore } from '../../../core/stores/current-journey.store';
import {
  CurrentJourneyDto,
  DriverByCodeResponse,
  DriverSearchResultDto,
  PaymentMethod,
  PayTripRequest,
  PayTripResponse
} from '../../../core/models/passenger.models';
import { NodeDto } from '../../../core/models/driver.models';
import { RouteTimelineComponent } from '../../../shared/components/route-timeline/route-timeline.component';
import { ReceiptDialogComponent } from '../../../shared/components/receipt-dialog/receipt-dialog.component';

@Component({
  selector: 'app-pay-trip',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatAutocompleteModule,
    RouteTimelineComponent
  ],
  templateUrl: './pay-trip.component.html',
  styleUrl: './pay-trip.component.scss'
})
export class PayTripComponent implements OnInit, OnDestroy {
  auth = inject(AuthService);
  passengerService = inject(PassengerService);
  liveSync = inject(LiveSyncService);
  walletStore = inject(WalletStore);
  currentJourneyStore = inject(CurrentJourneyStore);
  route = inject(ActivatedRoute);
  dialog = inject(MatDialog);
  snackBar = inject(MatSnackBar);

  @ViewChild(MatAutocompleteTrigger) autoTrigger?: MatAutocompleteTrigger;

  PaymentMethod = PaymentMethod;

  driverCodeInput = '';
  selectedFromNode = '';
  selectedToNode = '';
  selectedPaymentMethod: PaymentMethod = PaymentMethod.Telebirr;

  isLoadingDriver = signal(false);
  isSearchingDrivers = signal<boolean>(false);
  driverInfo = signal<DriverByCodeResponse | null>(null);
  driverSuggestions = signal<DriverSearchResultDto[]>([]);
  isPaying = signal<boolean>(false);
  fareAmount = signal<number>(0);
  completedReceipt = signal<PayTripResponse | null>(null);

  driverError = signal<string | null>(null);
  boardingError = signal<string | null>(null);
  alightError = signal<string | null>(null);
  paymentError = signal<string | null>(null);

  orderedNodes: NodeDto[] = [];
  boardingNodeOptions: NodeDto[] = [];
  alightNodeOptions: NodeDto[] = [];
  activeDirectionDisplay = '';

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;
  private tripStatusSub?: Subscription;
  private currentWatchedDriverCode: string | null = null;

  paymentOptions = [
    { id: PaymentMethod.Telebirr, name: 'Telebirr', sub: 'Instant Mobile Pay', icon: 'smartphone', color: '#0284c7' },
    { id: PaymentMethod.Wallet, name: 'TaxiPay Wallet', sub: 'Direct Balance', icon: 'account_balance_wallet', color: '#f59e0b' },
    { id: PaymentMethod.CbeBirr, name: 'CBE Birr', sub: 'Bank Integration', icon: 'account_balance', color: '#a855f7' },
    { id: PaymentMethod.Card, name: 'Debit / Card', sub: 'Visa / Mastercard', icon: 'credit_card', color: '#10b981' },
    { id: PaymentMethod.Cash, name: 'Cash to Driver', sub: 'Physical Currency', icon: 'money', color: '#94a3b8' }
  ];

  ngOnInit(): void {
    // Set up debounced search for driver code autocomplete from real database
    this.searchSub = this.searchSubject.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(query => {
        const trimmed = query.trim();
        if (!trimmed) {
          this.isSearchingDrivers.set(false);
          this.driverSuggestions.set([]);
          return [];
        }
        this.isSearchingDrivers.set(true);
        return this.passengerService.searchDrivers(trimmed);
      })
    ).subscribe({
      next: results => {
        this.isSearchingDrivers.set(false);
        this.driverSuggestions.set(results);
      },
      error: () => {
        this.isSearchingDrivers.set(false);
        this.driverSuggestions.set([]);
      }
    });

    // Real-time trip status synchronization (e.g. driver starts/completes trip while passenger is viewing)
    this.tripStatusSub = this.liveSync.tripStatusUpdated$.subscribe(() => {
      const code = this.driverCodeInput.trim() || this.driverInfo()?.driverId;
      if (code) {
        this.refreshDriverLiveState(code);
      }
    });

    this.route.queryParams.subscribe(params => {
      if (params['driver']) {
        this.driverCodeInput = params['driver'].trim();
        this.lookupDriver();
      }
    });
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
    this.tripStatusSub?.unsubscribe();
    if (this.currentWatchedDriverCode) {
      this.liveSync.leaveDriverGroup(this.currentWatchedDriverCode);
    }
  }

  onDriverCodeInput(): void {
    this.driverError.set(null);
    const val = this.driverCodeInput.trim();
    if (!val) {
      if (this.currentWatchedDriverCode) {
        this.liveSync.leaveDriverGroup(this.currentWatchedDriverCode);
        this.currentWatchedDriverCode = null;
      }
      this.driverInfo.set(null);
      this.driverSuggestions.set([]);
      this.selectedFromNode = '';
      this.selectedToNode = '';
      this.orderedNodes = [];
      this.boardingNodeOptions = [];
      this.alightNodeOptions = [];
      this.fareAmount.set(0);
      this.boardingError.set(null);
      this.alightError.set(null);
      this.paymentError.set(null);
      this.autoTrigger?.closePanel();
    } else {
      this.searchSubject.next(val);
    }
  }

  onDriverOptionSelected(event: MatAutocompleteSelectedEvent): void {
    const selectedCode = event.option.value;
    this.driverCodeInput = selectedCode;
    this.autoTrigger?.closePanel();
    this.lookupDriver(selectedCode);
  }

  lookupDriver(codeToLookup?: string): void {
    const code = (codeToLookup || this.driverCodeInput).trim();
    this.autoTrigger?.closePanel();

    if (!code) {
      this.driverError.set('Please enter a valid driver code.');
      this.driverInfo.set(null);
      return;
    }

    this.driverError.set(null);
    this.isLoadingDriver.set(true);

    this.passengerService.getDriverByCode(code).subscribe({
      next: driver => {
        this.driverInfo.set(driver);
        this.isLoadingDriver.set(false);
        this.driverError.set(null);
        this.autoTrigger?.closePanel();

        // Manage SignalR real-time group for this driver
        if (this.currentWatchedDriverCode && this.currentWatchedDriverCode !== driver.driverId) {
          this.liveSync.leaveDriverGroup(this.currentWatchedDriverCode);
        }
        this.currentWatchedDriverCode = driver.driverId;
        this.liveSync.joinDriverGroup(driver.driverId);

        // Get driver's exact ordered nodes from the database
        this.orderedNodes = driver.routeNodes || [];
        if (this.orderedNodes.length >= 2) {
          this.activeDirectionDisplay = `${this.orderedNodes[0].name} (${this.orderedNodes[0].id}) → ${this.orderedNodes[this.orderedNodes.length - 1].name} (${this.orderedNodes[this.orderedNodes.length - 1].id})`;
        } else {
          this.activeDirectionDisplay = driver.routeName || 'Active Corridor';
        }

        // Populate directional options
        this.updateDirectionalStops();
      },
      error: () => {
        this.isLoadingDriver.set(false);
        this.driverInfo.set(null);
        this.selectedFromNode = '';
        this.selectedToNode = '';
        this.orderedNodes = [];
        this.boardingNodeOptions = [];
        this.alightNodeOptions = [];
        this.fareAmount.set(0);
        this.autoTrigger?.closePanel();
        this.driverError.set(`Driver with code "${code}" does not exist.`);
        this.snackBar.open(`Driver with code "${code}" does not exist.`, 'Dismiss', {
          duration: 4000,
          panelClass: 'snack-error'
        });
      }
    });
  }

  private refreshDriverLiveState(code: string): void {
    this.passengerService.getDriverByCode(code).subscribe({
      next: driver => {
        this.driverInfo.set(driver);
        if (driver.hasActiveTrip) {
          this.snackBar.open(`⚡ Live update: Driver is on an active trip and accepting passengers!`, 'OK', {
            duration: 3500,
            panelClass: 'snack-success'
          });
        }
      },
      error: () => {}
    });
  }

  updateDirectionalStops(): void {
    if (!this.orderedNodes.length) return;

    // Boarding options: All stops except the last terminal stop
    this.boardingNodeOptions = this.orderedNodes.slice(0, this.orderedNodes.length - 1);

    // Default boarding to first stop if current selected is invalid
    const fromIdx = this.orderedNodes.findIndex(n => n.id === this.selectedFromNode);
    if (fromIdx < 0 || fromIdx >= this.orderedNodes.length - 1) {
      this.selectedFromNode = this.orderedNodes[0].id;
    }

    // Refresh alight options strictly ahead of boarding stop
    this.refreshAlightOptions();
  }

  refreshAlightOptions(): void {
    const fromIdx = this.orderedNodes.findIndex(n => n.id === this.selectedFromNode);
    if (fromIdx >= 0) {
      // Alight options are STRICTLY stops that appear AFTER the boarding stop in direction of travel
      this.alightNodeOptions = this.orderedNodes.slice(fromIdx + 1);

      // If current alight selection is before or equal to boarding, default to next stop
      const toIdx = this.orderedNodes.findIndex(n => n.id === this.selectedToNode);
      if (toIdx <= fromIdx) {
        this.selectedToNode = this.orderedNodes.length > fromIdx + 1 ? this.orderedNodes[fromIdx + 1].id : '';
      }
    } else {
      this.alightNodeOptions = [];
      this.selectedToNode = '';
    }
    this.computeFare();
  }

  onBoardingNodeSelected(fromId: string): void {
    this.selectedFromNode = fromId;
    this.boardingError.set(null);
    this.alightError.set(null);
    this.refreshAlightOptions();
  }

  onAlightNodeSelected(toId: string): void {
    this.selectedToNode = toId;
    this.alightError.set(null);
    this.computeFare();
  }

  onPaymentMethodSelected(methodId: PaymentMethod): void {
    this.selectedPaymentMethod = methodId;
    this.paymentError.set(null);
  }

  onTimelineFromSelected(nodeId: string): void {
    const fromIdx = this.orderedNodes.findIndex(n => n.id === nodeId);
    if (fromIdx < this.orderedNodes.length - 1) {
      this.selectedFromNode = nodeId;
      this.boardingError.set(null);
      this.alightError.set(null);
      this.refreshAlightOptions();
    }
  }

  onTimelineToSelected(nodeId: string): void {
    const fromIdx = this.orderedNodes.findIndex(n => n.id === this.selectedFromNode);
    const toIdx = this.orderedNodes.findIndex(n => n.id === nodeId);
    if (toIdx > fromIdx) {
      this.selectedToNode = nodeId;
      this.alightError.set(null);
      this.computeFare();
    }
  }

  computeFare(): void {
    const fromIdx = this.orderedNodes.findIndex(n => n.id === this.selectedFromNode);
    const toIdx = this.orderedNodes.findIndex(n => n.id === this.selectedToNode);

    if (fromIdx >= 0 && toIdx > fromIdx) {
      const hops = toIdx - fromIdx;
      this.fareAmount.set(Math.max(5, hops * 5));
    } else {
      this.fareAmount.set(0);
    }
  }

  isSelectionValid(): boolean {
    const fromIdx = this.orderedNodes.findIndex(n => n.id === this.selectedFromNode);
    const toIdx = this.orderedNodes.findIndex(n => n.id === this.selectedToNode);
    return fromIdx >= 0 && toIdx > fromIdx;
  }

  getNodeName(id: string): string {
    const node = this.orderedNodes.find(n => n.id === id);
    return node ? node.name : `Station ${id}`;
  }

  getMethodName(method: PaymentMethod): string {
    return this.paymentOptions.find(m => m.id === method)?.name || 'Digital';
  }

  executePayment(): void {
    this.driverError.set(null);
    this.boardingError.set(null);
    this.alightError.set(null);
    this.paymentError.set(null);

    let hasError = false;

    if (!this.driverInfo()) {
      this.driverError.set('Please enter a valid driver code.');
      hasError = true;
    }

    if (!this.selectedFromNode) {
      this.boardingError.set('Please select a boarding point.');
      hasError = true;
    }

    if (!this.selectedToNode) {
      this.alightError.set('Please select an alighting point.');
      hasError = true;
    } else if (this.selectedFromNode && !this.isSelectionValid()) {
      this.alightError.set('Alighting point must be after the boarding point.');
      hasError = true;
    }

    if (this.selectedPaymentMethod === undefined || this.selectedPaymentMethod === null) {
      this.paymentError.set('Please select a payment method.');
      hasError = true;
    }

    if (hasError) return;

    const user = this.auth.currentUser();
    const passengerId = user?.id;
    if (!passengerId) {
      this.snackBar.open('Please sign in as a passenger to pay for your trip.', 'Sign In', { duration: 4000, panelClass: 'snack-warning' });
      return;
    }

    this.isPaying.set(true);
    const driver = this.driverInfo()!;

    const req: PayTripRequest = {
      driverId: driver.driverId,
      boardingNode: this.selectedFromNode,
      destinationNode: this.selectedToNode,
      paymentMethod: this.selectedPaymentMethod
    };

    this.passengerService.payForTrip(passengerId, req).subscribe({
      next: receipt => {
        this.isPaying.set(false);
        this.snackBar.open('🎉 Payment successful! You are added to the driver manifest in real time.', 'OK', {
          duration: 4500,
          panelClass: 'snack-success'
        });

        // Ensure receipt reflects exact calculated fare
        const receiptWithExactFare: PayTripResponse = {
          ...receipt,
          fare: this.fareAmount(),
          boardingNode: `${this.getNodeName(this.selectedFromNode)} (${this.selectedFromNode})`,
          destinationNode: `${this.getNodeName(this.selectedToNode)} (${this.selectedToNode})`
        };

        // Persist confirmed paid receipt state in signal
        this.completedReceipt.set(receiptWithExactFare);

        // Update centralized stores
        if (receipt.newBalance !== undefined) {
          this.walletStore.setBalance(receipt.newBalance);
        } else {
          this.walletStore.loadBalance(passengerId);
        }

        // Set active current journey in store
        const currentJourney: CurrentJourneyDto = {
          journeyId: receipt.tripId,
          tripId: receipt.tripId,
          driverId: driver.driverId,
          driverCode: driver.driverId,
          driverName: driver.fullName,
          driverPhone: driver.phone,
          routeName: driver.routeName,
          boardingPoint: this.getNodeName(this.selectedFromNode),
          alightingPoint: this.getNodeName(this.selectedToNode),
          fare: this.fareAmount(),
          paymentMethod: this.getMethodName(this.selectedPaymentMethod),
          paymentStatus: 'Completed',
          alightingStatus: 'OnBoard',
          tripStatus: 'Active',
          boardedAt: receipt.paidAt || new Date().toISOString()
        };
        this.currentJourneyStore.setJourney(currentJourney);

        // Open official receipt modal
        this.dialog.open(ReceiptDialogComponent, {
          data: receiptWithExactFare,
          width: '440px',
          panelClass: 'glass-dialog'
        });
      },
      error: err => {
        this.isPaying.set(false);
        this.handleBackendError(err);
      }
    });
  }

  openCompletedReceiptModal(): void {
    const r = this.completedReceipt();
    if (!r) return;
    this.dialog.open(ReceiptDialogComponent, {
      data: r,
      width: '440px',
      panelClass: 'glass-dialog'
    });
  }

  resetBooking(): void {
    this.completedReceipt.set(null);
    this.driverCodeInput = '';
    this.driverInfo.set(null);
    this.driverSuggestions.set([]);
    this.orderedNodes = [];
    this.boardingNodeOptions = [];
    this.alightNodeOptions = [];
    this.selectedFromNode = '';
    this.selectedToNode = '';
    this.fareAmount.set(0);
    this.driverError.set(null);
    this.boardingError.set(null);
    this.alightError.set(null);
    this.paymentError.set(null);
  }

  handleBackendError(err: any): void {
    const errors = err?.error?.errors;
    if (errors && typeof errors === 'object') {
      if (errors['Request.DriverId'] || errors['DriverId']) {
        this.driverError.set(errors['Request.DriverId']?.[0] || errors['DriverId']?.[0]);
      }
      if (errors['Request.BoardingNode'] || errors['BoardingNode']) {
        this.boardingError.set(errors['Request.BoardingNode']?.[0] || errors['BoardingNode']?.[0]);
      }
      if (errors['Request.DestinationNode'] || errors['DestinationNode']) {
        this.alightError.set(errors['Request.DestinationNode']?.[0] || errors['DestinationNode']?.[0]);
      }
      if (errors['Request.PaymentMethod'] || errors['PaymentMethod']) {
        this.paymentError.set(errors['Request.PaymentMethod']?.[0] || errors['PaymentMethod']?.[0]);
      }
      if (errors['Request'] || errors['']) {
        const generalMsg = errors['Request']?.[0] || errors['']?.[0];
        if (generalMsg?.toLowerCase().includes('boarding') || generalMsg?.toLowerCase().includes('destination')) {
          this.alightError.set(generalMsg);
        }
      }
    } else {
      const msg = err?.error?.Message || err?.error?.detail || err?.error?.message || 'Payment transaction failed. Please check your balance.';
      this.snackBar.open(msg, 'Dismiss', { duration: 4500, panelClass: 'snack-error' });
    }
  }
}
