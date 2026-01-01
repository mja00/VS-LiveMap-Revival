import * as L from 'leaflet';

import { Marker } from './Marker';
import { Point } from '../../data/Point';

import type { MarkersLayer } from '../MarkersLayer';
import type { MarkerJson } from './Marker';

export class Circle extends Marker {
	constructor(layer: MarkersLayer, json: MarkerJson) {
		super(layer, json, L.circle(Point.of(json.point).toLatLng(), {
			...json.options,
			radius: Circle.radius(json),
		}));
	}

	public override update(json: MarkerJson): void {
		super.update(json);
		(this._marker as L.Circle)
			.setLatLng(Point.of(json.point).toLatLng())
			.setRadius(Circle.radius(json));
	}

	private static radius(json: MarkerJson): number {
		return Point.pixelsToMeters(json.options.radius ?? 10);
	}
}
