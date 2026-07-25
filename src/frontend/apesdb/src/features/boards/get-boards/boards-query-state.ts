import { inferParserType, parseAsInteger, parseAsString } from "nuqs";

export const boardFilterParsers = {
  search: parseAsString.withDefault(""),
  page: parseAsInteger.withDefault(1),
};

export type BoardFilters = inferParserType<typeof boardFilterParsers>;
export type BoardFilterPatch = Partial<BoardFilters>;
