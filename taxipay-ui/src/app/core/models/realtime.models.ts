export interface TripStatusUpdateDto {
  tripId: string;
  status: string;
  direction?: string;
  tripStart?: string;
  tripEnd?: string;
}

export interface DriverStatusUpdateDto {
  driverId: string;
  status: string;
  isVerified: boolean;
  driverCode?: string;
  rejectionReason?: string;
}

export interface RouteChangeNotificationDto {
  driverId: string;
  newRouteId?: string;
  newRouteCode?: string;
  newRouteName?: string;
  approved: boolean;
  note?: string;
  rejectionReason?: string;
}

export interface PassengerRouteRequestNotificationDto {
  requestId: string;
  passengerId: string;
  status: string;
  adminNote?: string;
}
