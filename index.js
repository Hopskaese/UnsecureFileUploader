var app = require('http').createServer(handler)
var io = require('socket.io')(app);
var fs = require('fs');
var g_socket = ''

app.listen(3000);

function handler (req, res) {
  fs.readFile(__dirname + '/index.html',
  function (err, data) {
    if (err) {
      res.writeHead(500);
      return res.end('Error loading index.html');
    }
    res.writeHead(200);
    // response.write(data) , response.end();
    res.end(data);
  });
}

io.on('connection', function(socket){
  g_socket = socket;
  console.log('a user connected');
  
  socket.on('disconnect', function(){
    console.log('user disconnected');
  });

  socket.on('error', function(err) {
    console.log("Error: "+ err);
  });

  socket.on('SendFile', function(name, binaryData) {
   fs.writeFile(name, binaryData, function(err) {
    if(err)
      g_socket.emit("Error saving file to server");
    else
      g_socket.emit("SuccessReceive");
    });
  });

  socket.on('GetFiles', function(data){
    ReadAndSendFile(data);
  });

  socket.on("GetFolders", function(data) {
    var folders = GetFolderList();
  });
});

function ReadAndSendFile(filenames)
{
  var cnt = -1;
  var data_arr = [];
  for (var i =0; i < filenames.length; i++)
  {
    fs.readFile("1/"+filenames[i], function(err, data){
      cnt++;
      if (err)
      {
        console.log("error on read");
        data[cnt] = "";
      }
      else
      {
        data_arr[cnt] = data;
      }

      if(cnt == filenames.length -1)
      {
        console.log("sending data Length of data array: ", data_arr.length);
        g_socket.emit("ReceiveFile", [filenames.length, filenames, data_arr]);
      }
  });
  }
}

function GetFolderList()
{
  fs.readdir("1/", function(err, data) {
    if (err) throw err;

    g_socket.emit("FolderList", data);
  });
}


