import { describe, expect, it } from 'vitest';

import { ArrayUtils } from '../src/util/ArrayUtils';

describe('ArrayUtils', () => {
	describe('remove', () => {
		it('should remove a single item from the array', () => {
			const arr = ['a', 'b', 'c'];
			ArrayUtils.remove(arr, 'b');
			expect(arr).toEqual(['a', 'c']);
		});

		it('should remove multiple occurrences of an item', () => {
			const arr = ['a', 'b', 'a', 'b', 'c'];
			ArrayUtils.remove(arr, 'b');
			expect(arr).toEqual(['a', 'a', 'c']);
		});

		it('should do nothing if the item is not in the array', () => {
			const arr = ['a', 'b', 'c'];
			ArrayUtils.remove(arr, 'd');
			expect(arr).toEqual(['a', 'b', 'c']);
		});

		it('should handle empty arrays', () => {
			const arr: string[] = [];
			ArrayUtils.remove(arr, 'a');
			expect(arr).toEqual([]);
		});
	});

	it('should not pollute Array.prototype', () => {
		expect((Array.prototype as any).remove).toBeUndefined();
	});
});
