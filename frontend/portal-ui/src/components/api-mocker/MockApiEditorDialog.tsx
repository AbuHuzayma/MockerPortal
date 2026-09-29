import { useState } from "react";
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  MenuItem,
  FormControlLabel,
  Switch,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Typography,
  IconButton,
  Stack,
  Divider,
  Box,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import AddIcon from "@mui/icons-material/Add";
import {
  type MockApiDto,
  type MockEndpointDto,
  type MockResponseDto,
  type MockMatchRuleDto,
  emptyEndpoint,
  emptyResponse,
  emptyMatchRule,
} from "../../api/mockAdmin";

const HTTP_METHODS = ["GET", "POST", "PUT", "PATCH", "DELETE"];
const MATCH_SOURCES = ["Header", "QueryString", "Path", "RequestBody"] as const;
const MATCH_OPERATORS = ["Equals", "NotEquals", "Contains", "StartsWith"] as const;
const ENVIRONMENTS = ["DEV", "QA", "PREPROD"];

interface MockApiEditorDialogProps {
  open: boolean;
  initial: MockApiDto;
  isNew: boolean;
  isSaving: boolean;
  onClose: () => void;
  onSave: (dto: MockApiDto) => void;
}

export function MockApiEditorDialog({ open, initial, isNew, isSaving, onClose, onSave }: MockApiEditorDialogProps) {
  const [dto, setDto] = useState<MockApiDto>(initial);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{isNew ? "New Mock API" : `Edit ${initial.code}`}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Stack direction="row" spacing={2}>
            <TextField
              label="Code"
              value={dto.code}
              onChange={(e) => setDto({ ...dto, code: e.target.value.toUpperCase() })}
              disabled={!isNew}
              fullWidth
              helperText="Unique identifier used in the /mock/{code}/... serving route"
            />
            <TextField
              select
              label="Environment"
              value={dto.environment}
              onChange={(e) => setDto({ ...dto, environment: e.target.value })}
              sx={{ minWidth: 160 }}
            >
              {ENVIRONMENTS.map((env) => (
                <MenuItem key={env} value={env}>
                  {env}
                </MenuItem>
              ))}
            </TextField>
          </Stack>

          <TextField label="Name" value={dto.name} onChange={(e) => setDto({ ...dto, name: e.target.value })} fullWidth />
          <TextField
            label="Description"
            value={dto.description ?? ""}
            onChange={(e) => setDto({ ...dto, description: e.target.value })}
            fullWidth
            multiline
            minRows={2}
          />

          <Divider />
          <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
            <Typography variant="h6">Endpoints</Typography>
            <Button
              startIcon={<AddIcon />}
              onClick={() => setDto({ ...dto, endpoints: [...dto.endpoints, emptyEndpoint()] })}
            >
              Add endpoint
            </Button>
          </Stack>

          {dto.endpoints.length === 0 && (
            <Typography variant="body2" color="text.secondary">
              No endpoints configured yet — add one above.
            </Typography>
          )}

          {dto.endpoints.map((endpoint, endpointIndex) => (
            <EndpointEditor
              key={endpointIndex}
              endpoint={endpoint}
              onChange={(next) => {
                const endpoints = [...dto.endpoints];
                endpoints[endpointIndex] = next;
                setDto({ ...dto, endpoints });
              }}
              onRemove={() => setDto({ ...dto, endpoints: dto.endpoints.filter((_, i) => i !== endpointIndex) })}
            />
          ))}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={isSaving}>
          Cancel
        </Button>
        <Button variant="contained" onClick={() => onSave(dto)} disabled={isSaving || !dto.code || !dto.name}>
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function EndpointEditor({
  endpoint,
  onChange,
  onRemove,
}: {
  endpoint: MockEndpointDto;
  onChange: (next: MockEndpointDto) => void;
  onRemove: () => void;
}) {
  return (
    <Accordion>
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        <Typography sx={{ flexGrow: 1 }}>
          {endpoint.httpMethod} /{endpoint.path || "(path)"} — {endpoint.responses.length} response(s)
        </Typography>
      </AccordionSummary>
      <AccordionDetails>
        <Stack spacing={2}>
          <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
            <TextField
              select
              label="Method"
              value={endpoint.httpMethod}
              onChange={(e) => onChange({ ...endpoint, httpMethod: e.target.value })}
              sx={{ minWidth: 140 }}
            >
              {HTTP_METHODS.map((m) => (
                <MenuItem key={m} value={m}>
                  {m}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Path"
              value={endpoint.path}
              onChange={(e) => onChange({ ...endpoint, path: e.target.value })}
              helperText="No leading slash, e.g. verify/national-id"
              fullWidth
            />
            <FormControlLabel
              control={<Switch checked={endpoint.isActive} onChange={(e) => onChange({ ...endpoint, isActive: e.target.checked })} />}
              label="Active"
            />
            <IconButton onClick={onRemove} aria-label="Remove endpoint">
              <DeleteOutlineIcon />
            </IconButton>
          </Stack>

          <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
            <Typography variant="subtitle1">Responses</Typography>
            <Button
              size="small"
              startIcon={<AddIcon />}
              onClick={() =>
                onChange({ ...endpoint, responses: [...endpoint.responses, emptyResponse(endpoint.responses.length + 1)] })
              }
            >
              Add response
            </Button>
          </Stack>

          {endpoint.responses.map((response, responseIndex) => (
            <ResponseEditor
              key={responseIndex}
              response={response}
              onChange={(next) => {
                const responses = [...endpoint.responses];
                responses[responseIndex] = next;
                onChange({ ...endpoint, responses });
              }}
              onRemove={() => onChange({ ...endpoint, responses: endpoint.responses.filter((_, i) => i !== responseIndex) })}
            />
          ))}
        </Stack>
      </AccordionDetails>
    </Accordion>
  );
}

function ResponseEditor({
  response,
  onChange,
  onRemove,
}: {
  response: MockResponseDto;
  onChange: (next: MockResponseDto) => void;
  onRemove: () => void;
}) {
  return (
    <Box sx={{ border: "1px solid", borderColor: "divider", borderRadius: 1, p: 2 }}>
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
          <TextField label="Name" value={response.name} onChange={(e) => onChange({ ...response, name: e.target.value })} fullWidth />
          <TextField
            label="Status"
            type="number"
            value={response.httpStatusCode}
            onChange={(e) => onChange({ ...response, httpStatusCode: Number(e.target.value) })}
            sx={{ width: 130 }}
          />
          <TextField
            label="Priority"
            type="number"
            value={response.priority}
            onChange={(e) => onChange({ ...response, priority: Number(e.target.value) })}
            sx={{ width: 130 }}
            helperText="Lower first"
          />
          <TextField
            label="Delay (ms)"
            type="number"
            value={response.delayMilliseconds}
            onChange={(e) => onChange({ ...response, delayMilliseconds: Number(e.target.value) })}
            sx={{ width: 140 }}
          />
          <FormControlLabel
            control={<Switch checked={response.isActive} onChange={(e) => onChange({ ...response, isActive: e.target.checked })} />}
            label="Active"
          />
          <IconButton onClick={onRemove} aria-label="Remove response">
            <DeleteOutlineIcon />
          </IconButton>
        </Stack>

        <TextField
          label="Response headers (JSON)"
          value={response.responseHeaders ?? ""}
          onChange={(e) => onChange({ ...response, responseHeaders: e.target.value })}
          fullWidth
        />
        <TextField
          label="Response body"
          value={response.responseBody ?? ""}
          onChange={(e) => onChange({ ...response, responseBody: e.target.value })}
          fullWidth
          multiline
          minRows={3}
          helperText="Supports {{request.header.X}}, {{request.query.X}}, {{request.body.X}}, {{now}}, {{uuid}} — plain text substitution only, never code."
        />

        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
          <Typography variant="body2" color="text.secondary">
            Match rules {response.matchRules.length === 0 ? "(none — this is the default/fallback response)" : ""}
          </Typography>
          <Button
            size="small"
            startIcon={<AddIcon />}
            onClick={() => onChange({ ...response, matchRules: [...response.matchRules, emptyMatchRule()] })}
          >
            Add rule
          </Button>
        </Stack>

        {response.matchRules.map((rule, ruleIndex) => (
          <MatchRuleEditor
            key={ruleIndex}
            rule={rule}
            onChange={(next) => {
              const matchRules = [...response.matchRules];
              matchRules[ruleIndex] = next;
              onChange({ ...response, matchRules });
            }}
            onRemove={() => onChange({ ...response, matchRules: response.matchRules.filter((_, i) => i !== ruleIndex) })}
          />
        ))}
      </Stack>
    </Box>
  );
}

function MatchRuleEditor({
  rule,
  onChange,
  onRemove,
}: {
  rule: MockMatchRuleDto;
  onChange: (next: MockMatchRuleDto) => void;
  onRemove: () => void;
}) {
  return (
    <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
      <TextField
        select
        label="Source"
        value={rule.source}
        onChange={(e) => onChange({ ...rule, source: e.target.value as MockMatchRuleDto["source"] })}
        sx={{ minWidth: 140 }}
        size="small"
      >
        {MATCH_SOURCES.map((s) => (
          <MenuItem key={s} value={s}>
            {s}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        label="Field"
        value={rule.field}
        onChange={(e) => onChange({ ...rule, field: e.target.value })}
        size="small"
        helperText="Header/query/body key (ignored for Path)"
      />
      <TextField
        select
        label="Operator"
        value={rule.operator}
        onChange={(e) => onChange({ ...rule, operator: e.target.value as MockMatchRuleDto["operator"] })}
        sx={{ minWidth: 140 }}
        size="small"
      >
        {MATCH_OPERATORS.map((op) => (
          <MenuItem key={op} value={op}>
            {op}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        label="Expected value"
        value={rule.expectedValue}
        onChange={(e) => onChange({ ...rule, expectedValue: e.target.value })}
        size="small"
        fullWidth
      />
      <IconButton onClick={onRemove} aria-label="Remove rule" size="small">
        <DeleteOutlineIcon fontSize="small" />
      </IconButton>
    </Stack>
  );
}
