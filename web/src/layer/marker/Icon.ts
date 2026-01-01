import * as L from 'leaflet';

import { Marker } from './Marker';
import { Point } from '../../data/Point';

import type { MarkersLayer } from '../MarkersLayer';
import type { MarkerJson } from './Marker';

export class Icon extends Marker {
	private static svgHtml: string = '<svg preserveAspectRatio=\'none\' width=\'100%\' height=\'100%\'><use href=\'{0}\'></use></svg>';

	constructor(layer: MarkersLayer, json: MarkerJson) {
		super(layer, json, L.marker(Point.of(json.point).toLatLng(), {
			...json.options,
			icon: json.options?.iconUrl?.startsWith('#svg-') ?
				L.divIcon({
					...json.options,
					className: '',
					html: Icon.svgHtml.replace('{0}', json.options.iconUrl),
				}) :
				L.icon(json.options as L.IconOptions),
		}));
	}

	public override update(json: MarkerJson): void {
		super.update(json);
		const icon: L.Marker = this._marker as L.Marker;
		icon.setLatLng(Point.of(json.point).toLatLng());
		Object.assign(icon.options, json.options);
		Object.assign(icon.options.icon!.options, json.options);
		//(this._marker as L.Marker).getElement()?.setAttribute('aria-label', icon.options?.title ?? icon.options.alt ?? '');
	}
}
