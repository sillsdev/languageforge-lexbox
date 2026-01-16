export default {
  files: ['.'],
  formats: [
    {
      name: 'stylish',
      output: 'console',
    },
    {
      name: 'json',
      output: 'file',
      path: 'eslint_report.json',
    },
  ],
};
