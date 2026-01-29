import React, { useState, useEffect } from 'react';
import { api } from '../api';
import type { CountrySummary } from '../api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
interface CountryListProps {
  onSelectCountry: (country: CountrySummary['country']) => void;
}

export const CountryList: React.FC<CountryListProps> = ({ onSelectCountry }) => {
  const [countries, setCountries] = useState<CountrySummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchCountries = async () => {
      try {
        setLoading(true);
        const data = await api.getCountrySummaries();
        setCountries(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch countries');
      } finally {
        setLoading(false);
      }
    };

    fetchCountries();
  }, []);

  if (loading) return (
    <div className="flex-1 overflow-y-auto p-5">
      <p className="text-muted-foreground">Loading countries...</p>
    </div>
  );
  if (error) return (
    <div className="flex-1 overflow-y-auto p-5">
      <p className="text-destructive">Error: {error}</p>
    </div>
  );

  return (
    <div className="flex-1 overflow-y-auto p-5">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-semibold">Empires</h2>
        <Badge variant="secondary">{countries.length}</Badge>
      </div>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-1">
        {[...countries]
          .sort((a, b) => (a.country.victoryRank || 0) - (b.country.victoryRank || 0))
          .map(({ country, incomeTotals }) => (
          <Card
            key={country.id}
            className="cursor-pointer border-border bg-card transition hover:bg-muted/70"
            onClick={() => onSelectCountry(country)}
          >
            <CardHeader className="pb-2">
              <CardTitle className="text-base">{country.name}</CardTitle>
              <p className="text-xs italic text-muted-foreground">{country.adjective}</p>
            </CardHeader>
            <CardContent className="pt-0">
              <div className="flex flex-col gap-1 text-xs text-muted-foreground">
                <span className="text-red-400">Military: {country.militaryPower.toFixed(2)}</span>
                <span className="text-emerald-400">Economy: {country.economyPower.toFixed(2)}</span>
                <span className="text-blue-400">Tech: {country.techPower.toFixed(2)}</span>
                <span className="text-yellow-400">Population: {country.numSapientPops.toLocaleString()}</span>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
};
