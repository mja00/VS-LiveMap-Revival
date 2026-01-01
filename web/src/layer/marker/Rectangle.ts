import * as L from 'leaflet';

import { Marker } from './Marker';
import { Point } from '../../data/Point';

import type { MarkersLayer } from '../MarkersLayer';
import type { MarkerJson } from './Marker';

export class Rectangle extends Marker {
	constructor(layer: MarkersLayer, json: MarkerJson) {
		super(layer, json, L.rectangle(L.latLngBounds(Point.toLatLngArray(json.points) as L.LatLng[]), json.options));
	}

	public override update(json: MarkerJson): void {
		super.update(json);
		(this._marker as L.Rectangle).setBounds(L.latLngBounds(Point.toLatLngArray(json.points) as L.LatLng[]));
	}
}
