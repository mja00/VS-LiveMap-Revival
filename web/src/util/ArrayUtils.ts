export const ArrayUtils = {
	/**
	 * Removes all occurrences of a specific object from an array.
	 * @param array The array to modify.
	 * @param obj The object to remove.
	 */
	remove<T>(array: T[], obj: T): void {
		let index: number;
		while ((index = array.indexOf(obj)) !== -1) {
			array.splice(index, 1);
		}
	},
};
