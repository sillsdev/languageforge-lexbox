module.exports = {
  tabWidth: 2,
  trailingComma: 'all',
  endOfLine: 'lf',
  singleQuote: true,
  overrides: [
    {
      files: '*.json',
      options: { parser: 'json' },
    },
  ],
};
