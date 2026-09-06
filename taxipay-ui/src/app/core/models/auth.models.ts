import { DriverSummaryDto } from './driver.models';
import { PassengerResponse } from './passenger.models';
import { AdminProfile } from './admin.models';

export type UserRole = 'driver' | 'passenger' | 'admin' | 'guest';

export interface UserProfile {
  id: string;
  driverCode?: string;
  firstName: string;
  lastName: string;
  phone: string;
  email?: string;
  role: UserRole;
  token: string;
  tokenExpiresAt?: string;
  driverData?: DriverSummaryDto;
  passengerData?: PassengerResponse;
  adminData?: AdminProfile;
}

export interface JwtClaims {
  nameid?: string;
  name?: string;
  email?: string;
  mobilephone?: string;
  role?: string;
  adminRole?: string;
  driverId?: string;
  exp?: number;
  nbf?: number;
  iat?: number;
  iss?: string;
  aud?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone'?: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
}
