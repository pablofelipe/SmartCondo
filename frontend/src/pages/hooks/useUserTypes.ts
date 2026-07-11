import { useEffect, useState } from 'react';
import config from '../../config';
import { getAuthHeaders } from '../../utils/ApiUtils';

interface UserType {
  id: number;
  name: string;
  description: string;
}

export const useUserTypes = () => {
  const [userTypes, setUserTypes] = useState<Record<string, UserType>>({});
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      const headers = getAuthHeaders();

      if (!headers.Authorization) {
        return;
      }

      var fullUrl = `${config.apiUrl}/UserType`;

      //console.log(`Searching url: ${fullUrl}`);

      const response = await fetch(fullUrl, {
        headers: headers,
      });

      const data: UserType[] = await response.json();

      const typesMap = data.reduce((acc, type) => {
        acc[type.name] = type;
        return acc;
      }, {} as Record<string, UserType>);

      setUserTypes(typesMap);
      setLoading(false);
    };

    fetchData();
  }, []);

  const getNameFromId = (id: number) =>
    Object.values(userTypes).find((t) => t.id === id)?.name;

  const getIdFromName = (name: string) => userTypes[name]?.id;

  return { userTypes, loading, getNameFromId, getIdFromName };
};
