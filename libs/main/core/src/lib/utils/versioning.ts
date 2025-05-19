// compare version 1.0.0 vs 1.0.1
export const compareVersions = (newVersion: string, currentVersion: string) => {
  const v1 = newVersion.split('.');
  const v2 = currentVersion.split('.');
  for (let i = 0; i < v1.length; i++) {
    if (v1[i] > v2[i]) return 1;
  }
  return 0;
};