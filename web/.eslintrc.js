module.exports = {
  extends: ['@nodecraft/eslint-config/typescript'],
  parserOptions: {
    project: './tsconfig.eslint.json',
    tsconfigRootDir: __dirname,
  },
  rules: {
    'id-length': 'off',
    'node/no-unsupported-features/node-builtins': 'off',
    '@typescript-eslint/no-use-before-define': 'off',
    'unicorn/prefer-dom-node-append': 'off',
    'unicorn/prefer-dom-node-text-content': 'off',
    'unicorn/prefer-add-event-listener': 'off',
    'unicorn/no-array-for-each': 'off',
    'no-return-assign': 'off',
  },
};
