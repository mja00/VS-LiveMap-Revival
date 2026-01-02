import { describe, expect, it } from 'vitest';

describe('Example Test', () => {
	it('should add two numbers correctly', () => {
		expect(1 + 1).toBe(2);
	});

	it('should pass a basic truthy check', () => {
		expect(true).toBeTruthy();
	});
});
