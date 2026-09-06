import { NodeDto } from './driver.models';

export enum PaymentMethod {
  Cash = 0,
  Wallet = 1,
  Telebirr = 2,
  CbeBirr = 3,
  Card = 4
}

export interface PassengerSummaryDto {
  fullName: string;
  phone: string;
  walletBalance: number;
  rating: number;
}

export interface RecentTripDto {
  tripId: string;
  direction: string;
  routeName: string;
  driverName: string;
  driverId: string;
  fare: number;
  tripStart: string;
  status: string;
}

export interface FavoriteDto {
  id?: string;
  name: string;
  routeCode: string;
  routeName?: string;
  fromNode: string;
  toNode: string;
  fare?: number;
}

export interface PassengerStatsDto {
  totalTrips: number;
  totalSpent: number;
  averageFare: number;
  tripsThisMonth: number;
  spentThisMonth: number;
}

export interface CurrentJourneyDto {
  journeyId: string;
  tripId: string;
  driverId: string;
  driverCode: string;
  driverName: string;
  driverPhone?: string;
  routeName: string;
  boardingPoint: string;
  alightingPoint: string;
  fare: number;
  paymentMethod: string;
  paymentStatus: string;
  alightingStatus: 'OnBoard' | 'AwaitingConfirmation' | 'Alighted' | string;
  tripStatus: string;
  boardedAt: string;
}

export interface PassengerDashboardResponse {
  summary: PassengerSummaryDto;
  currentJourney?: CurrentJourneyDto | null;
  recentTrips: RecentTripDto[];
  favorites: FavoriteDto[];
  stats: PassengerStatsDto;
}

export interface PassengerResponse {
  id: string;
  firstName: string;
  lastName: string;
  phone: string;
  walletBalance?: number;
  totalTrips?: number;
  totalSpent?: number;
  rating?: number;
  isVerified?: boolean;
  createdAt?: string;
}

export interface PassengerProfileResponse {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone: string;
  walletBalance: number;
  totalTrips: number;
  totalSpent: number;
  rating: number;
  isVerified: boolean;
  createdAt: string;
}

export interface UpdatePassengerProfileRequest {
  firstName: string;
  lastName: string;
  phone: string;
}

export interface TopUpWalletRequest {
  amount: number;
  paymentMethod?: string;
}

export interface TopUpWalletResponse {
  passengerId: string;
  amount: number;
  newBalance: number;
  transactionRef: string;
  topUpTime: string;
}

export interface PassengerLoginRequest {
  phone: string;
  password: string;
}

export interface PassengerLoginResponse {
  accessToken: string;
  refreshToken?: string;
  tokenExpiresAt?: string;
  loginTime?: string;
  passenger: PassengerResponse;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  phone: string;
  password: string;
}

export interface PayTripRequest {
  driverId: string;
  boardingNode: string;
  destinationNode: string;
  paymentMethod: PaymentMethod;
}

export interface PayTripResponse {
  tripId: string;
  passengerId: string;
  driverId: string;
  driverName: string;
  routeName: string;
  boardingNode: string;
  destinationNode: string;
  fare: number;
  paymentMethod: string;
  paymentStatus: string;
  paidAt: string;
  receiptNumber: string;
  newBalance?: number;
}

export interface DriverByCodeResponse {
  driverId: string;
  fullName: string;
  phone: string;
  rating: number;
  photoUrl?: string;
  routeName: string;
  routeCode: string;
  routeNodes: NodeDto[];
  isActive: boolean;
  hasActiveShift: boolean;
  hasActiveTrip?: boolean;
  activeTripId?: string;
  activeTripDirection?: string;
  activeTripDirectionDisplay?: string;
}

export interface DriverSearchResultDto {
  driverId: string;
  fullName: string;
  routeName: string;
  routeCode: string;
  rating: number;
  photoUrl?: string;
  isActive: boolean;
  hasActiveShift: boolean;
}

export interface FareResponse {
  fromNode: string;
  toNode: string;
  fare: number;
  routeName: string;
  routeCode: string;
  isValidRoute: boolean;
}

export interface TripHistoryItemDto {
  tripId: string;
  direction: string;
  routeName: string;
  driverName: string;
  driverId: string;
  boardingNode: string;
  destinationNode: string;
  boardingPointName?: string;
  destinationPointName?: string;
  fare: number;
  paymentMethod: string;
  paymentStatus: string;
  transactionRef?: string;
  tripStart: string;
  tripEnd?: string;
  status: string;
}

export interface TripHistoryResponse {
  trips: TripHistoryItemDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PassengerRouteRequestDto {
  id: string;
  passengerId: string;
  passengerName: string;
  passengerPhone: string;
  fromLocation: string;
  toLocation: string;
  description?: string;
  status: 'Pending' | 'Approved' | 'Rejected' | string;
  requestedAt: string;
  reviewedAt?: string;
  adminNotes?: string;
}

export interface CreatePassengerRouteRequestDto {
  fromLocation: string;
  toLocation: string;
  description?: string;
}
