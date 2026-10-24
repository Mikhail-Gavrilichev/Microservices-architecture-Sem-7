import { useCallback, useState } from "react";

const STORAGE_KEY = "tournament.juryMemberId";

export function useJuryId() {
  const [juryId, setJuryIdState] = useState(
    () => localStorage.getItem(STORAGE_KEY) ?? "",
  );

  const setJuryId = useCallback((value: string) => {
    setJuryIdState(value);

    if (value.trim()) {
      localStorage.setItem(STORAGE_KEY, value.trim());
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }, []);

  return [juryId, setJuryId] as const;
}
