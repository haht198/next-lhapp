export class ArrayUtils {
  static arrayToMap<T>(array: T[], key: keyof T) {
    return array.reduce((acc, item) => {
      acc[String(item[key])] = item;
      return acc;
    }, {} as Record<string, T>);
  }
}
