export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions: string[];
  environment: string;
}

export interface LoginResponseData {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  user: CurrentUser;
}
