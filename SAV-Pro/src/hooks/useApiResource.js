import { useCallback, useEffect, useState } from 'react';
import { getFriendlyApiError } from '../utils/errorMessages';

export function useApiResource(loader, { immediate = true, initialData = null } = {}) {
  const [data, setData] = useState(initialData);
  const [loading, setLoading] = useState(Boolean(immediate));
  const [error, setError] = useState('');
  const [errorStatus, setErrorStatus] = useState(0);

  const reload = useCallback(async () => {
    setLoading(true);
    setError('');
    setErrorStatus(0);
    try {
      const result = await loader();
      setData(result);
      return result;
    } catch (err) {
      setError(getFriendlyApiError(err));
      setErrorStatus(err?.status || 0);
      throw err;
    } finally {
      setLoading(false);
    }
  }, [loader]);

  useEffect(() => {
    if (!immediate) {
      setLoading(false);
      return undefined;
    }
    let active = true;
    setLoading(true);
    setError('');
    setErrorStatus(0);
    loader()
      .then(result => {
        if (active) setData(result);
      })
      .catch(err => {
        if (active) {
          setError(getFriendlyApiError(err));
          setErrorStatus(err?.status || 0);
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [loader, immediate]);

  return { data, setData, loading, error, errorStatus, reload };
}
