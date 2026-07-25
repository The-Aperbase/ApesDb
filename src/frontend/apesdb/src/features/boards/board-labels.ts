export function gameCountLabel(count: number): string {
  if (count === 1) {
    return "1 game";
  }

  return `${count} games`;
}
