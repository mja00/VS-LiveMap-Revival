/* eslint-disable @typescript-eslint/no-var-requires, unicorn/prefer-logical-operator-over-ternary */
const path = require('node:path');

const CssMinimizerPlugin = require('css-minimizer-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const TerserPlugin = require('terser-webpack-plugin');

module.exports = {
	devServer: {
		headers: {
			'Cache-Control': 'no-store',
		},
		static: {
			directory: path.join(__dirname, 'public'),
			watch: {
				ignored: '**/*.json',
				usePolling: false,
			},
		},
		historyApiFallback: {
			publicPath: '/',
			rewrites: [
				{
					from: /(.*\/)?(.+)\/([+-]?\d+)\/([+-]?\d+)\/([+-]?\d+)(\/.*)?/,
					to: ctx => (ctx.match[6] ? ctx.match[6] : '/'),
				},
				{
					from: /./,
					to: '/404.html',
				},
			],
		},
	},
	devtool: 'source-map',
	entry: [
		'./src/LiveMap.ts',
		'./src/lib/L.ellipse.js',
		'./src/lib/L.rotated.js',
	],
	externals: {
		leaflet: 'L',
	},
	mode: 'development',
	module: {
		rules: [
			{
				test: /\.ts$/i,
				use: 'ts-loader',
				include: [path.resolve(__dirname, 'src')],
			},
			{
				test: /\.s?css$/i,
				use: [
					'style-loader',
					{
						loader: MiniCssExtractPlugin.loader,
						options: {
							esModule: false,
						},
					},
					{
						loader: 'css-loader',
						options: {
							sourceMap: true,
							url: false,
						},
					},
					{
						loader: 'sass-loader',
						options: {
							sassOptions: {
								outputStyle: 'expanded',
							},
						},
					},
				],
			},
			{
				test: /\.svg$/,
				loader: 'svg-sprite-loader',
				options: {
					symbolId: filePath => `svg-${path.basename(filePath, '.svg')}`,
				},
			},
		],
	},
	optimization: {
		minimize: true,
		minimizer: [
			new CssMinimizerPlugin(),
			new TerserPlugin({
				extractComments: false,
				terserOptions: {
					format: {
						comments: false,
					},
				},
			}),
		],
	},
	output: {
		publicPath: '/',
		filename: 'livemap.js',
		path: path.resolve(__dirname, 'public'),
	},
	performance: {
		maxEntrypointSize: 512000,
		maxAssetSize: 512000,
	},
	resolve: {
		extensions: ['.ts', '.js', '.scss', '.css'],
	},
	plugins: [
		new MiniCssExtractPlugin({
			filename: 'livemap.css',
		}),
	],
};
