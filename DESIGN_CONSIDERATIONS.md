### Business considerations

- Documentation says "A merchant should be able to retrieve the details of a previously made payment" (only Authorized?) but in the "Retrieving a payment’s details" section it says "Must be one of the following values Authorized, Declined", so I'm saving both to the database.

- "The response should include a masked card number and card details along with a status code which indicates the result of the payment." This sounded a little bit confusing and different from the table and predefined classes. I followed the table and classes, which made more sense. I would reach out to PO to clarify.

- Would ask PO if Amount can be zero (CC validations).

- Regarding credit card validation, I would ask about leading zeros in the number and if the month/year combination must be in the future, or if it can accept the current month.

- The API doesn't provide a way to differentiate "Service Unavailable" and "Unauthorized" responses received from the bank simulator, but I'm assuming that this won't be returned to the merchant/shopper.

### Main technical considerations

- Changed Payments in repository to private readonly.
- Added `IPaymentsRepository` interface to test exceptions thrown from the database and the API responses in those cases.
- Created Payment entity and set it in the repository, to prevent changes in the API leaking to the database schema.
- Added appsettings to obtain AcquirerBank's baseAddress.
- Updated vulnerable libraries.
- Using default Visual Studio rules for naming/formatting.
- Using `sealed` whenever possible to convey intent.
- Added exception interception, with output formatted as ProblemDetails.

### Other remarks

- Some [JsonProperty] are not necessary, but I prefer to be clear as it makes it easier to maintain (less guessing).
- Regarding logging, the main points would be external interactions, in this case, repository and Acquiring Bank.
- For the RetrievesAPaymentSuccessfully test, I would ask the team about the use of `Random` and if we can refactor to something more deterministic.
- Possible improvement: Add testcontainers for CI/CD to automate the startup of the bank simulator.
- 422 (UnprocessableEntity) for bank declines and bank unavailable is not the best semantically, but it's the closest.
- In a production scenario, we will likely want to save "authorization_code" received from the bank.
