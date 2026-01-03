import type { LiveMap } from './LiveMap';
import type * as L from 'leaflet';


declare global {
	interface Window {
		livemap: LiveMap;

		fetchJson<T>(url: string): Promise<T>;

		createSVGIcon(icon: string): DocumentFragment;
	}

	interface Array<T> {
		remove(obj: T): void;
	}
}

module 'leaflet' {
	export namespace Browser {
		const linux: boolean;
	}

	interface MarkerOptions extends L.InteractiveLayerOptions {
		iconUrl?: string;
		radii?: L.PointTuple;
		radius?: number;
		rotationAngle?: number;
		color?: string;
	}

	interface MarkerClusterOptions {
		animate?: bool | undefined;
		animateAddingMarkers?: bool | undefined;
		clusterPane?: string | undefined;
		disableClusteringAtZoom?: number | undefined;
		iconCreateFunction?: ((cluster: L.MarkerCluster)=> L.DivIcon);
		maxClusterRadius?: number | undefined;
		polygonOptions?: L.PolylineOptions | undefined;
		removeOutsideVisibleBounds?: bool | undefined;
		singleMarkerMode?: bool | undefined;
		showCoverageOnHover?: bool | undefined;
		spiderfyDistanceMultiplier?: number | undefined;
		spiderfyOnMaxZoom?: bool | undefined;
		spiderfyShapePositions?: ((count: number)=> L.Point[]);
		spiderLegPolylineOptions?: L.PolylineOptions | undefined;
		zoomToBoundsOnClick?: bool | undefined;
	}

	export function markerClusterGroup(MarkerClusterOptions): L.MarkerCluster;

	interface MarkerCluster extends L.Layer {
		getChildCount(): number;

		getAllChildMarkers(): L.Marker[] | undefined;
	}

	export function ellipse(latLng: L.LatLngExpression, options?: L.EllipseOptions): L.Ellipse;

	interface Ellipse extends L.Path {
		setRadius(radii: L.PointTuple): this;

		getRadius(): L.Point;

		setTilt(tilt: number): this;

		getBounds(): L.LatLngBounds;

		getLatLng(): L.LatLng;

		setLatLng(latLng: L.LatLngExpression): this;
	}

	interface EllipseOptions extends L.PathOptions {
		radii?: L.PointTuple | undefined;
		tilt?: number;
	}
}
