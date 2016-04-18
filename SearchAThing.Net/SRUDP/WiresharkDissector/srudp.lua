-- SRUDP dissector, Copyright(C) 2016 Lorenzo Delana, License under MIT

-- The MIT License(MIT)
-- Copyright(c) 2016 Lorenzo Delana, https://searchathing.com
--
-- Permission is hereby granted, free of charge, to any person obtaining a
-- copy of this software and associated documentation files (the "Software"),
-- to deal in the Software without restriction, including without limitation
-- the rights to use, copy, modify, merge, publish, distribute, sublicense,
-- and/or sell copies of the Software, and to permit persons to whom the
-- Software is furnished to do so, subject to the following conditions:
--
-- The above copyright notice and this permission notice shall be included in
-- all copies or substantial portions of the Software.
--
-- THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
-- IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
-- FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
-- THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
-- LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
-- FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
-- DEALINGS IN THE SOFTWARE.

-----------------------------------------------------------------------------
-- PROTOCOL DISSECTOR
-----------------------------------------------------------------------------
local srudp = Proto("SRUDP", "SRUDP Protocol")

-- off: 0 - len: 1
local pf_opcode             = ProtoField.new("OpCode", "srudp.opcode", ftypes.UINT8)
local pf_opcode_connect     = ProtoField.bool("srudp.opcode_bits.connect", "Connect", 8, nil, 1, "connect")
local pf_opcode_ack         = ProtoField.bool("srudp.opcode_bits.ack", "Ack", 8, nil, 2, "ack")
local pf_opcode_data        = ProtoField.bool("srudp.opcode_bits.data", "Data", 8, nil, 4, "data")
local pf_opcode_disconnect  = ProtoField.bool("srudp.opcode_bits.disconnect", "Disconnect", 8, nil, 8, "disconnect")

-- off: 1 - len: 2
local pf_id = ProtoField.new("ID", "srudp.id", ftypes.UINT16)

-- off: 3 - len: 2
local pf_data_len = ProtoField.new("DataLen", "srudp.data_len", ftypes.UINT16)

-- off: 5 - len: 2
local pf_data_len_left = ProtoField.new("DataLenLeft", "srudp.data_len_left", ftypes.UINT16)

-- off: 7 - len: pktLen-7
local pf_data = ProtoField.new("Data", "studp.data", ftypes.STRING)

srudp.fields =
{
  pf_opcode, pf_opcode_connect, pf_opcode_ack, pf_opcode_data, pf_opcode_cont, pf_opcode_disconnect,
  pf_id,
  pf_data_len, pf_data_len_left,
  pf_data
}

-- Dissector function
function srudp.dissector(tvbuf, pktinfo, root)
  pktinfo.cols.protocol:set("SRUDP")   
  local pktlen = tvbuf:reported_length_remaining()    
  local tree = root:add(dns, tvbuf:range(0, pktlen))
    
  tree:set_text("SRUDP")  
  
  -- opcode
  local opcode_bits = tvbuf:range(0,1)
  local opcode_bits_tree = tree:add(pf_opcode, opcode_bits)
  opcode_bits_tree:add(pf_opcode_connect, opcode_bits)
  opcode_bits_tree:add(pf_opcode_ack, opcode_bits)
  opcode_bits_tree:add(pf_opcode_data, opcode_bits)
  opcode_bits_tree:add(pf_opcode_cont, opcode_bits)
  opcode_bits_tree:add(pf_opcode_disconnect, opcode_bits)

  -- id
  tree:add(pf_id, tvbuf:range(1,2))  
  
  -- data_len  
  tree:add(pf_data_len, tvbuf:range(3,2))
  
  -- data_len_left
  tree:add(pf_data_len_left, tvbuf:range(5,2))
  
  -- data
  if pktlen>7 then tree:add(pf_data, "[".. tvbuf:range(7, pktlen-7):string() .."]") end
  
  -- info column
  local opcode = tvbuf:range(0, 1):uint()
  local id = tvbuf:range(1, 2):uint()  
  local flags = ""
  
  if bit32.btest(opcode, 1) then flags = "Connect" end
  if bit32.btest(opcode, 2) then flags = "Ack" end
  if bit32.btest(opcode, 4) then flags = "Data" end  
  if bit32.btest(opcode, 8) then flags = "Disconnect" end
  pktinfo.cols.info:set("ID ".. id .." (".. flags ..")")
       
  return pos
end

-- Associate dissector
DissectorTable.get("udp.port"):add(50000, srudp)
