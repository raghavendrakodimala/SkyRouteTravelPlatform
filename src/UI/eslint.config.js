// @ts-check
// ESLint 9 flat config for the SkyRoute Angular 22 app.
// Toolchain: @eslint/js + typescript-eslint + angular-eslint (Angular 22 line).
// Scope: src/**/*.ts (TypeScript) and src/**/*.html (Angular templates).
const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

module.exports = tseslint.config(
  {
    // Global ignores (applies to every config below).
    ignores: ['dist/**', 'node_modules/**', '.angular/**', 'coverage/**'],
  },
  {
    // TypeScript sources: JS recommended + typescript-eslint recommended +
    // angular-eslint recommended, with the Angular inline-template processor
    // so component inline templates are linted with the template rules too.
    files: ['src/**/*.ts'],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'app', style: 'camelCase' },
      ],
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'app', style: 'kebab-case' },
      ],
      // Honor the codebase's established "leading underscore = intentionally
      // unused" convention (e.g. interface params kept for callers). Real
      // unused symbols without the underscore still error.
      '@typescript-eslint/no-unused-vars': [
        'error',
        {
          argsIgnorePattern: '^_',
          varsIgnorePattern: '^_',
          caughtErrorsIgnorePattern: '^_',
        },
      ],
    },
  },
  {
    // Angular HTML templates: angular-eslint template recommended.
    files: ['src/**/*.html'],
    extends: [...angular.configs.templateRecommended],
    rules: {},
  },
);
