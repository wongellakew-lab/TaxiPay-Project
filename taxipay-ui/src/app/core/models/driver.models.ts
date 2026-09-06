export enum DriverStatus {
  Active = 0,
  Pending = 1,
  Suspended = 2
}

export interface NodeDto {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}

export interface RouteSummaryDto {
  id: string;
  name: string;
  routeCode: string;
}

export interface DriverSummaryDto {
  id: string;
  driverId: string;
  fullName: string;
  phone: string;
  photoUrl?: string;
  status: DriverStatus;
  rating: number;
  routeName?: string;
  routeCode?: string;
  routeNodes: NodeDto[];
}

export interface ShiftStatusDto {
  shiftId: string;
  shiftStart: string;
  currentTripNumber: number;
  totalPassengers: number;
  totalRevenue: number;
  status: string;
}

export interface DriverLoginResponse {
  accessToken: string;
  refreshToken: string;
  tokenExpiresAt: string;
  loginTime: string;
  sessionId: string;
  isFirstLogin: boolean;
  requiresPasswordChange: boolean;
  hasPendingRouteChange: boolean;
  driver: DriverSummaryDto;
  activeShift?: ShiftStatusDto;
}

export interface ActivePassengerDto {
  journeyId: string;
  passengerId: string;
  passengerName: string;
  passengerPhone: string;
  boardingNode: string;
  destinationNode: string;
  fare: number;
  paymentMethod: string;
  paymentStatus: string;
  alightingStatus?: string;
  boardedAt: string;
}

export interface DailySummaryDto {
  todayTrips: number;
  todayRevenue: number;
  todayPassengers: number;
  paymentBreakdown?: PaymentBreakdownDto;
}

export interface RouteDirectionDto {
  directionKey: string;
  displayLabel: string;
  fromNodeId: string;
  fromNodeName: string;
  toNodeId: string;
  toNodeName: string;
  isForward: boolean;
}

export interface ActiveTripDto {
  tripId: string;
  tripNumber: number;
  routeName: string;
  routeCode: string;
  direction: string;
  directionDisplay: string;
  tripStart: string;
  status: string;
  passengersCount: number;
  tripRevenue: number;
}

export interface CompletedTripDto {
  tripId: string;
  tripNumber: number;
  routeName: string;
  direction: string;
  tripStart: string;
  tripEnd?: string;
  status: string;
  passengersCount: number;
  tripRevenue: number;
}

export interface StartTripRequest {
  direction: string;
}

export interface DriverDashboardResponse {
  driverId: string;
  fullName: string;
  routeName: string;
  routeCode: string;
  activeShiftId?: string;
  activeTripId?: string;
  isTripActive?: boolean;
  shiftStart: string;
  currentTripNumber: number;
  totalRevenue: number;
  totalPassengers: number;
  availableSeats: number;
  capacity: number;
  activePassengers: ActivePassengerDto[];
  activeTrip?: ActiveTripDto | null;
  lastCompletedTrip?: CompletedTripDto | null;
  routeNodes?: NodeDto[];
  availableDirections?: RouteDirectionDto[];
  todaySummary: DailySummaryDto;
}

export interface DriverResponse {
  id: string;
  driverId?: string;
  firstName: string;
  lastName: string;
  phone: string;
  licenseNumber: string;
  photoUrl?: string;
  rating: number;
  totalTrips: number;
  totalRevenue: number;
  status: DriverStatus;
  isVerified: boolean;
  verifiedAt?: string;
  lastLoginAt?: string;
  currentRouteId?: string;
  currentRouteName?: string;
  createdAt: string;
}

export interface TripResponse {
  tripId: string;
  shiftId: string;
  driverId: string;
  driverName: string;
  tripNumber: number;
  direction: string;
  routeName: string;
  tripStart: string;
  tripEnd?: string;
  passengersCount: number;
  tripRevenue: number;
  status: string;
}

export interface PaymentBreakdownDto {
  cash: number;
  digital: number;
  telebirr?: number;
  wallet?: number;
}

export interface TripSummaryDto {
  tripId: string;
  tripNumber: number;
  direction: string;
  passengerCount: number;
  tripRevenue: number;
  startedAt: string;
  completedAt?: string;
}

export interface ShiftSummaryResponse {
  shiftId: string;
  shiftStart: string;
  shiftEnd?: string;
  status: string;
  totalPassengers: number;
  totalRevenue: number;
  totalTrips: number;
  averageFare: number;
  paymentBreakdown: PaymentBreakdownDto;
  trips: TripSummaryDto[];
}

export interface RouteChangeResponse {
  requestId: string;
  driverId?: string;
  driverName?: string;
  currentRoute?: RouteSummaryDto | null;
  currentRouteName?: string;
  requestedRoute?: RouteSummaryDto | null;
  requestedRouteId?: string;
  requestedRouteName: string;
  reason?: string;
  status: string;
  requestedAt?: string;
  reviewedAt?: string;
  rejectionReason?: string;
}

export interface CreateDriverRequest {
  firstName: string;
  lastName: string;
  phone: string;
  licenseNumber: string;
  password: string;
  currentRouteId?: string;
  preferredRouteId?: string;
}

export interface DriverLoginRequest {
  phone: string;
  password: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  phone: string;
  photoUrl?: string;
}

export interface RequestRouteChangeRequest {
  newRouteId?: string;
  requestedRouteId?: string;
  reason: string;
}

export interface CompleteTripRequest {
  latitude: number;
  longitude: number;
}
