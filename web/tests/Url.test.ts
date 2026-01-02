import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { Url } from '../src/data/Url';
import { LiveMap } from '../src/LiveMap';

// Mock Point to keep tests isolated
vi.mock('../src/data/Point', () => {
	return {
		Point: {
			of: (x: any, z: any) => ({ x: Number(x || 0), z: Number(z || 0) })
		}
	};
});

describe('Url', () => {
	let mockLiveMap: any;

	beforeEach(() => {
		mockLiveMap = {
			settings: {
				zoom: { def: 2 },
				renderers: [{ id: 'basic' }],
				friendlyUrls: false
			}
		};
		// Mock window.location
		Object.defineProperty(window, 'location', {
			value: {
				pathname: '/',
				search: '',
			},
			writable: true
		});
	});

	it('should parse values from regex match in URL string', () => {
		// Regex: (.*\/)?(.+)\/([+-]?\d+)\/([+-]?\d+)\/([+-]?\d+)(\/.*)?$
		// Example: http://host/map/basic/3/100/200
		const urlString = 'http://localhost/map/basic/3/100/200';

		const url = new Url(mockLiveMap, urlString);

		expect(url.renderer).toBe('basic');
		expect(url.zoom).toBe(3);
		expect(url.x).toBe(100);
		expect(url.z).toBe(200);
	});

	it('should fallback to defaults if URL does not match regex and no query params', () => {
		const url = new Url(mockLiveMap, 'http://localhost/');

		expect(url.renderer).toBe('basic');
		expect(url.zoom).toBe(2); // default
		expect(url.x).toBe(0);
		expect(url.z).toBe(0);
	});

	it('should parse query parameters if regex does not match', () => {
		window.location.search = '?renderer=cave&zoom=4&x=50&z=60';
		const url = new Url(mockLiveMap, 'http://localhost/'); // non-matching string to force fallback?
		// Wait, the constructor logic: if (renderer) ... else { regex match ... if (!match) usage window.location }
		// We pass 'http://localhost/' as url. It might NOT match the regex depending on strictness.
		// Regex: `(.*\/)?(.+)\/([+-]?\d+)\/([+-]?\d+)\/([+-]?\d+)(\/.*)?$`
		// 'http://localhost/' does NOT match (expects numbers). So it falls back to window.location.

		expect(url.renderer).toBe('cave');
		expect(url.zoom).toBe(4);
		expect(url.x).toBe(50);
		expect(url.z).toBe(60);
	});

	it('should format toString() as query params by default', () => {
		const url = new Url(mockLiveMap, 'http://localhost/map/basic/3/100/200');
		// We mocked Point to return simple object
		expect(url.toString()).toContain('?renderer=basic&zoom=3&x=100&z=200');
	});

	it('should format toString() as friendly URL if enabled', () => {
		mockLiveMap.settings.friendlyUrls = true;
		const url = new Url(mockLiveMap, 'http://localhost/map/basic/5/10/20');
		expect(url.toString()).toContain('basic/5/10/20/');
	});
});
