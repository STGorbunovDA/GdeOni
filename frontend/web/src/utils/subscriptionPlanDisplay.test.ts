import { describe, it, expect } from 'vitest';
import { displaySubscriptionPlan } from './subscriptionPlanDisplay';

describe('displaySubscriptionPlan', () => {
  it('maps Monthly to human-readable label', () => {
    expect(displaySubscriptionPlan('Monthly')).toBe('На один месяц');
  });

  it.each([null, undefined, ''])(
    'returns empty string for empty-like input %j',
    (input) => {
      expect(displaySubscriptionPlan(input)).toBe('');
    },
  );

  it('returns unknown plan as-is (forward compatibility)', () => {
    expect(displaySubscriptionPlan('Yearly')).toBe('Yearly');
  });
});
