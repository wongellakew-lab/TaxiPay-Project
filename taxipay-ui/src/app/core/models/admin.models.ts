export interface AdminLoginRequest {
  email: string;
  password: string;
}

export interface AdminProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  role: string;
  isActive: boolean;
  lastLoginAt?: string;
}

export interface AdminLoginResponse {
  accessToken: string;
  refreshToken: string;
  tokenExpiresAt: string;
  admin: AdminProfile;
}

export interface AdminDriverListItem {
  id: string;
  driverId?: string;
  firstName: string;
  lastName: string;
  phone: string;
  licenseNumber: string;
  status: 'Pending' | 'Active' | 'Suspended' | 'Rejected' | string;
  isVerified: boolean;
  rating: number;
  totalTrips: number;
  totalRevenue: number;
  currentRouteId?: string;
  currentRouteCode?: string;
  currentRouteName?: string;
  preferredRouteId?: string;
  preferredRouteCode?: string;
  preferredRouteName?: string;
  rejectionReason?: string;
  createdAt: string;
}

export interface ApproveDriverRequest {
  assignedRouteId?: string;
}

export interface RejectDriverRequest {
  rejectionReason: string;
}

export interface AdminNode {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}

export interface RouteDetail {
  id: string;
  name: string;
  routeCode: string;
  nodes: AdminNode[];
  isActive: boolean;
  activeDriversCount: number;
  createdAt: string;
}

export interface CreateRouteRequest {
  name: string;
  routeCode: string;
  nodes: AdminNode[];
  isActive: boolean;
}

export interface UpdateRouteRequest {
  name: string;
  routeCode: string;
  nodes: AdminNode[];
  isActive: boolean;
}

export interface FareMatrixItem {
  id: string;
  routeId: string;
  routeCode: string;
  fromNode: string;
  toNode: string;
  fare: number;
  isActive: boolean;
}

export interface CreateFareMatrixRequest {
  routeId: string;
  fromNode: string;
  toNode: string;
  fare: number;
  isActive?: boolean;
}

export interface UpdateFareMatrixRequest {
  fare: number;
  isActive: boolean;
}

export interface AdminRouteChangeRequest {
  id: string;
  driverId: string;
  driverName: string;
  driverCode?: string;
  driverPhone: string;
  currentRouteId: string;
  currentRouteCode: string;
  currentRouteName: string;
  newRouteId: string;
  newRouteCode: string;
  newRouteName: string;
  reason: string;
  status: 'Pending' | 'Approved' | 'Rejected' | string;
  hasActiveTrip: boolean;
  requestedAt: string;
  reviewedAt?: string;
  rejectionReason?: string;
  reviewedByAdminName?: string;
}

export interface AdminDispute {
  id: string;
  tripId: string;
  tripNumber: string;
  passengerId: string;
  passengerName: string;
  passengerPhone: string;
  driverId: string;
  driverName: string;
  driverPhone: string;
  routeCode: string;
  routeName: string;
  fromNode: string;
  toNode: string;
  farePaid: number;
  disputeType: string;
  description: string;
  status: 'Open' | 'Closed' | string;
  resolutionNotes?: string;
  resolvedAt?: string;
  resolvedByAdminName?: string;
  createdAt: string;
}

export interface OpenDisputeRequest {
  notes?: string;
}

export interface CloseDisputeRequest {
  resolutionNotes: string;
  issueRefund?: boolean;
}

export interface DailyRevenueStat {
  date: string;
  totalRevenue: number;
  tripsCount: number;
}

export interface RecentActivity {
  id: string;
  type: string;
  title: string;
  description: string;
  timestamp: string;
  status: string;
}

export interface CorridorStat {
  routeCode: string;
  routeName: string;
  totalRevenue: number;
  revenueSharePercent: number;
  activeDriversCount: number;
  totalTripsCount: number;
  peakStation?: string;
}

export interface AdminAnalyticsOverview {
  totalRevenue: number;
  todayRevenue: number;
  totalTrips: number;
  activeTrips: number;
  totalDrivers: number;
  activeDriversOnShift: number;
  pendingDriverApprovals: number;
  totalPassengers: number;
  openDisputes: number;
  pendingRouteRequests: number;
  dailyRevenueHistory: DailyRevenueStat[];
  recentActivities: RecentActivity[];
  corridorStats?: CorridorStat[];
}
