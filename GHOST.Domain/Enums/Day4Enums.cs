namespace GHOST.Domain.Enums;

public enum DiscountType { Percentage, FixedAmount, FreeMinutes, FreeProduct, FreeSession, Gift }
public enum DiscountStatus { Requested, Approved, Rejected, Applied }
public enum LoyaltyTransactionType { Earn, Redeem, Adjustment, Expiration }
public enum GiftPeriod { Weekly, Monthly, VisitCount, SpendingAmount }
public enum GiftStatus { Issued, Redeemed }
public enum GiftCardStatus { Active, Used, Expired, Cancelled }
public enum GiftCardRewardType { CashValue, FreeHour, FreeDrink, Other }
