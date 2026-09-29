export interface Customer {
  custId: string;
  custNumber: string;
  t24CustomerId: string | null;
  mobileNo: string;
  firstName: string | null;
  lastName: string | null;
  arabicFirstName: string | null;
  arabicLastName: string | null;
  email: string | null;
  nationality: string | null;
  lifeStatus: string | null;
  blacklistStatus: string | null;
}

/** The shape carried across customer screens once a customer is selected — master spec §9. */
export interface CustomerContextValue {
  customerId: string;
  customerNumber: string;
  mobileNumber: string;
  t24CustomerId: string | null;
}

export function toCustomerContext(customer: Customer): CustomerContextValue {
  return {
    customerId: customer.custId,
    customerNumber: customer.custNumber,
    mobileNumber: customer.mobileNo,
    t24CustomerId: customer.t24CustomerId,
  };
}
