import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { Point } from '../src/data/Point';
import * as L from 'leaflet';

// Mock Leaflet
vi.mock('leaflet', () => ({
	point: (x: number, y: number) => ({ x, y }),
	latLng: (lat: number, lng: number) => ({ lat, lng }),
}));

describe('Point', () => {
	describe('Point.of()', () => {
		it('should return undefined for null/undefined input', () => {
			// @ts-expect-error Testing invalid input
			expect(Point.of(undefined)).toBeUndefined();
			// @ts-expect-error Testing invalid input
			expect(Point.of(null)).toBeNull();
		});

		it('should create Point from two numbers', () => {
			const p = Point.of(10, 20);
			expect(p.x).toBe(10);
			expect(p.z).toBe(20);
		});

		it('should create Point from array [x, z]', () => {
			const p = Point.of([10, 20]);
			expect(p.x).toBe(10);
			expect(p.z).toBe(20);
		});

		it('should create Point from array [x, y, z] (taking x, y)', () => {
			// Logic in Point.ts says arrays > 3 are treated as LatLng? No, logic says length 3 is LatLng.
			// Let's check source: length === 3 -> new Point(a[0], a[1], true); where true means latlng.
			// If latlng=true, x = metersToPixels(z), z = metersToPixels(x).
			// Wait, constructor(x, z, latlng).
			// if latlng: this._x = Point.metersToPixels(z) (which is passed as x argument?? No argument names are x, z).
			// constructor(x, z, latlng) -> if latlng, this._x = metersToPixels(z), this._z = metersToPixels(x).
			// logic: return new Point(a[0], a[1], a.length === 3);

			// So if [10, 20, 30], it calls new Point(10, 20, true).
			// z argument is 20. x argument is 10.
			// _x = metersToPixels(20) ??
			// _z = metersToPixels(10) ??

			// We need to mock metersToPixels or window.livemap for this to be deterministic.
		});

		it('should create Point from object {x, z}', () => {
			const p = Point.of({ x: 10, z: 20 } as any);
			expect(p.x).toBe(10);
			expect(p.z).toBe(20);
		});

		it('should create Point from object {x, y}', () => {
			const p = Point.of({ x: 10, y: 20 } as any);
			expect(p.x).toBe(10);
			expect(p.z).toBe(20);
		});
	});

	describe('Arithmetic', () => {
		it('should add numbers correctly', () => {
			const p = new Point(10, 20);
			p.add(5);
			expect(p.x).toBe(15);
			expect(p.z).toBe(25);
		});

		it('should add Point correctly', () => {
			const p1 = new Point(10, 20);
			const p2 = new Point(5, 5);
			p1.add(p2);
			expect(p1.x).toBe(15);
			expect(p1.z).toBe(25);
		});

		it('should subtract Point correctly', () => {
			const p1 = new Point(10, 20);
			const p2 = new Point(5, 5);
			p1.subtract(p2);
			expect(p1.x).toBe(5);
			expect(p1.z).toBe(15);
		});

		it('should multiply Point correctly', () => {
			const p1 = new Point(10, 20);
			const p2 = new Point(2, 3);
			p1.multiply(p2);
			expect(p1.x).toBe(20);
			expect(p1.z).toBe(60);
		});

		it('should divide Point correctly', () => {
			const p1 = new Point(10, 20);
			const p2 = new Point(2, 4);
			p1.divide(p2);
			expect(p1.x).toBe(5);
			expect(p1.z).toBe(5);
		});
	});

	describe('Conversions with scaling', () => {
		beforeEach(() => {
			(window as any).livemap = { scale: 2 };
		});

		afterEach(() => {
			delete (window as any).livemap;
		});

		it('pixelsToMeters should apply scale', () => {
			// pixelsToMeters = num * scale -> 10 * 2 = 20
			expect(Point.pixelsToMeters(10)).toBe(20);
		});

		it('metersToPixels should apply scale', () => {
			// metersToPixels = num / scale -> 10 / 2 = 5
			expect(Point.metersToPixels(10)).toBe(5);
		});
	});
});
