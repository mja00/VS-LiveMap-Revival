import * as L from 'leaflet';

export class Point {
	public static of(a: number | string | number[] | L.Point | L.LatLng | Point, b?: number | string): Point {
		if (a === undefined || a === null) {
			// fail fast
			return a;
		}
		if (Array.isArray(a) && a.length > 1) {
			// 2 length is L.PointTuple, 3 length is L.LatLngTuple
			return new Point(a[0], a[1], a.length === 3);
		}
		if (typeof a === 'object') {
			if ('x' in a) {
				// 'x' is in Point and L.Point
				if ('y' in a) {
					// 'y' is in L.Point
					return new Point(a.x, a.y);
				}
				if ('z' in a) {
					// 'z' is in Point
					return new Point(a.x, a.z);
				}
			}
			if ('lat' in a) {
				// 'lat' is in L.LatLng
				return new Point(a.lat, a.lng, true);
			}
		}
		// must be regular numbers
		if (typeof a === 'number' || typeof a === 'string') {
			return new Point(Number(a), Number(b ?? 0));
		}
		// guess not...
		return undefined!;
	}

	public static toLatLngArray(inArr: unknown[]): L.LatLng | L.LatLng[] {
		const outArr: L.LatLng[] = [];
		inArr.forEach((coord: unknown): void => {
			if (!Array.isArray(coord)) {
				// not a valid coordinate entry
				return;
			}
			if (Array.isArray(coord[0])) {
				// entry is a set of coordinates, must dig deeper
				outArr.push(this.toLatLngArray(coord) as L.LatLng);
				return;
			}
			const point: Point = Point.of(coord);
			if (point) {
				// coordinate is a valid Point
				outArr.push(point.toLatLng());
			}
		});
		return outArr;
	}

	public static pixelsToMeters(num: number): number {
		// todo - there's got to be a way to handle this in the CRS
		return num * (window.livemap?.scale ?? 1);
	}

	public static metersToPixels(num: number): number {
		// todo - there's got to be a way to handle this in the CRS
		return num / (window.livemap?.scale ?? 1);
	}

	private _x: number;
	private _z: number;

	constructor(x: number, z: number, latlng?: boolean) {
		this._x = (latlng ? Point.metersToPixels(z) : x) ?? 0;
		this._z = (latlng ? Point.metersToPixels(x) : z) ?? 0;
	}

	get x(): number {
		return this._x;
	}

	get z(): number {
		return this._z;
	}

	public add(n: number | Point): this {
		if (n instanceof Point) {
			this._x += n._x;
			this._z += n._z;
		} else {
			this._x += n;
			this._z += n;
		}
		return this;
	}

	public subtract(n: number | Point): this {
		if (n instanceof Point) {
			this._x -= n._x;
			this._z -= n._z;
		} else {
			this._x -= n;
			this._z -= n;
		}
		return this;
	}

	public multiply(n: number | Point): this {
		if (n instanceof Point) {
			this._x *= n._x;
			this._z *= n._z;
		} else {
			this._x *= n;
			this._z *= n;
		}
		return this;
	}

	public divide(n: number | Point): this {
		if (n instanceof Point) {
			this._x /= n._x;
			this._z /= n._z;
		} else {
			this._x /= n;
			this._z /= n;
		}
		return this;
	}

	public ceil(): this {
		this._x = Math.ceil(this._x);
		this._z = Math.ceil(this._z);
		return this;
	}

	public floor(): this {
		this._x = Math.floor(this._x);
		this._z = Math.floor(this._z);
		return this;
	}

	public round(): this {
		this._x = Math.round(this._x);
		this._z = Math.round(this._z);
		return this;
	}

	public toPoint(offset?: Point): L.Point {
		return L.point(
			this._x + (offset?._x ?? 0),
			this._z + (offset?._z ?? 0),
		);
	}

	public toLatLng(offset?: Point): L.LatLng {
		return L.latLng(
			Point.pixelsToMeters(this._z) + (offset?._z ?? 0),
			Point.pixelsToMeters(this._x) + (offset?._x ?? 0),
		);
	}

	public toString(format?: string): string {
		return (format || '{x}, {z}')
			.replace('{x}', `${this._x}`)
			.replace('{z}', `${this._z}`);
	}
}
