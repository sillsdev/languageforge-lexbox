-- Run by prod-collect.ps1: psql -q -v start=YYYY-MM-DD -v end=YYYY-MM-DD -f prod-queries.sql
-- Read-only, time-boxed, and prints one CSV row of counts. The FW Lite part scans the quarter's
-- CRDT commits once and only looks further back for the projects that were active, instead of
-- grouping every commit ever made.
\set ON_ERROR_STOP on
\pset format csv
BEGIN READ ONLY;
SET LOCAL statement_timeout = '120s';
SET LOCAL lock_timeout = '10s';

-- FW Lite: projects with a CRDT commit in the quarter, and whether that was their first ever.
-- Deleted projects stay in (their codes carry a __<timestamp> suffix).
WITH active AS MATERIALIZED (
  SELECT DISTINCT c."ProjectId"
  FROM public."CrdtCommits" c
  WHERE c."HybridDateTime_DateTime" >= :'start'
    AND c."HybridDateTime_DateTime" < :'end'
),
fwl AS (
  SELECT p."Code" AS code,
         NOT EXISTS (
           SELECT 1 FROM public."CrdtCommits" e
           WHERE e."ProjectId" = a."ProjectId"
             AND e."HybridDateTime_DateTime" < :'start'
         ) AS new_in_quarter
  FROM active a
  JOIN public."Projects" p ON p."Id" = a."ProjectId"
)
SELECT COUNT(*) AS fwl_active_projects,
       COUNT(*) FILTER (WHERE new_in_quarter) AS fwl_new_projects,
       COUNT(*) FILTER (WHERE code !~ 'test|train|demo|dev|copy') AS fwl_languages_impacted,
       -- Lexbox: projects created in the quarter, deleted ones included (created is created).
       (SELECT COUNT(*) FROM public."Projects"
        WHERE "CreatedDate" >= :'start' AND "CreatedDate" < :'end') AS lexbox_created_projects
FROM fwl;

ROLLBACK;
