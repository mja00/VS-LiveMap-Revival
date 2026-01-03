import * as L from 'leaflet';

import { Marker } from './Marker';
import { Point } from '../../data/Point';

import type { MarkersLayer } from '../MarkersLayer';
import type { MarkerJson } from './Marker';

export class Icon extends Marker {
	private static svgHtml: string = '<svg preserveAspectRatio=\'none\' width=\'100%\' height=\'100%\'{1}><use href=\'{0}\'></use></svg>';

	constructor(layer: MarkersLayer, json: MarkerJson) {
		const colorStyle: string = json.options?.color ? ` style='color:${json.options.color}'` : '';
		super(layer, json, L.marker(Point.of(json.point).toLatLng(), {
			...json.options,
			icon: json.options?.iconUrl?.startsWith('#svg-') ?
				L.divIcon({
					...json.options,
					className: '',
					html: Icon.svgHtml.replace('{0}', json.options.iconUrl).replace('{1}', colorStyle),
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

		// Update color if it's an SVG icon
		if (json.options?.iconUrl?.startsWith('#svg-') && json.options?.color) {
			const iconElement = icon.getElement();
			if (iconElement) {
				const svg = iconElement.querySelector('svg');
				if (svg) {
					svg.style.color = json.options.color;
				}
			}
		}
		//(this._marker as L.Marker).getElement()?.setAttribute('aria-label', icon.options?.title ?? icon.options.alt ?? '');
	}
}
