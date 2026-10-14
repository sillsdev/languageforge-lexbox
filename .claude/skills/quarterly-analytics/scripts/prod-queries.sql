-- Run by prod-collect.ps1: psql -v start=YYYY-MM-DD -v end=YYYY-MM-DD -v fwl_out=... -v lexbox_out=... -f prod-queries.sql
-- Read-only and time-boxed. The FW Lite query scans the quarter's CRDT commits once and only looks
-- further back for the projects that were active, instead of grouping every commit ever made.
\set ON_ERROR_STOP on
\pset format csv
BEGIN READ ONLY;
SET LOCAL statement_timeout = '120s';
SET LOCAL lock_timeout = '10s';

-- FW Lite: projects with a CRDT commit in the quarter, and whether that was their first ever.
-- Deleted projects stay in (their codes carry a __<timestamp> suffix).
\o :'fwl_out'
WITH active AS MATERIALIZED (
  SELECT DISTINCT c."ProjectId"
  FROM public."CrdtCommits" c
  WHERE c."HybridDateTime_DateTime" >= :'start'
    AND c."HybridDateTime_DateTime" < :'end'
)
SELECT p."Code" AS code,
       NOT EXISTS (
         SELECT 1 FROM public."CrdtCommits" e
         WHERE e."ProjectId" = a."ProjectId"
           AND e."HybridDateTime_DateTime" < :'start'
       ) AS new_in_quarter
FROM active a
JOIN public."Projects" p ON p."Id" = a."ProjectId"
ORDER BY p."Code";

-- Lexbox: projects created in the quarter, deleted ones included (created is created).
\o :'lexbox_out'
SELECT COUNT(*) AS created_in_quarter
FROM public."Projects"
WHERE "CreatedDate" >= :'start'
  AND "CreatedDate" < :'end';

\o
ROLLBACK;
