/***?
 * Follow info for a migration:
 * - 3 wif of the multisig
 * - file avm of new contract: location
 * - old contract script hash
 * - smart contract deployment option: name, version, author, email, description
 * - neoscan & neo-client
 */


const { default: Neon, api, wallet, tx, rpc, sc, u } = require("@cityofzion/neon-js");
const fs = require('fs');

/**
 * Read config
 */
let rawconfig = fs.readFileSync('config.json');
let config = JSON.parse(rawconfig);
//console.log(config); process.exit(0);
//neo clients
const neoscan = new api.neoscan.instance(
    config.neo.neo_scan
);
const rpcNodeUrl = config.neo.rpc_node;

//multisig key
const keyA = new wallet.Account(
    config.multisig.keyA
);
const keyB = new wallet.Account(
    config.multisig.keyB
);
const keyC = new wallet.Account(
    config.multisig.keyC
);

//contract
const avmFilePath = config.smart_contract.avmPath;
const oldContractScriptHash = config.smart_contract.oldContract;
const contract = config.smart_contract.name;
const version = config.smart_contract.version;
const author = config.smart_contract.author;
const email = config.smart_contract.email;
const description = config.smart_contract.description;
//console.log(avmFilePath,oldContractScriptHash,contract,version,author,email,description);
/**
 * End of config
 */



const multisigAcct = wallet.Account.createMultiSig(2, [
    keyA.publicKey,
    keyB.publicKey,
    keyC.publicKey
]);

console.log("\n\n--- Multi-sig ---");
console.log(`My multi-sig address is ${multisigAcct.address}`);
console.log(`My multi-sig verificationScript is ${multisigAcct.contract.script}`);



//build smart contract
let builder = Neon.create.scriptBuilder();

let newSmartContractScript = fs.readFileSync(avmFilePath).toString('hex');
//test if old === new
let newscript = u.reverseHex(u.hash160(newSmartContractScript));
console.log(newscript)
if(newscript == oldContractScriptHash) {
    
    console.error("Existed script hash");
    process.exit(1);
}
builder.emitAppCall(oldContractScriptHash, 'migrate', [sc.ContractParam.byteArray(newSmartContractScript),
    sc.ContractParam.integer(710),
    sc.ContractParam.integer(5),
    sc.ContractParam.integer(1),
    sc.ContractParam.string(contract), //name of smart contract
    sc.ContractParam.string(version), //version
    sc.ContractParam.string(author), //author
    sc.ContractParam.string(email), //email
    sc.ContractParam.string(description) //description
]);
//end build script 
var constructTx = neoscan.getBalance(multisigAcct.address).then(balanceVal => {
    let txconfig = {
        account: multisigAcct,
        api: neoscan,
        script:builder.str,
        gas:510,
        balance: balanceVal,
        fees: 10
    };
    return api.fillUrl(txconfig)
    .then(api.fillBalance)
    .then(api.createInvocationTx)
    .then(api.addAttributeIfExecutingAsSmartContract)
    .then(api.addAttributeForMintToken)
    .then(api.modifyTransactionForEmptyTransaction)
    .then(data => {console.log(data.tx);return data.tx;})
});
    
const signTx = constructTx.then(transaction => {
    const txHex = transaction.serialize(false);

    // This can be any 2 out of the 3 keys.
    const sig1 = wallet.sign(txHex, keyA.privateKey);
    const sig2 = wallet.sign(txHex, keyB.privateKey);


    const multiSigWitness = tx.Witness.buildMultiSig(
        txHex,
        [sig1, sig2],
        multisigAcct
    );

    transaction.addWitness(multiSigWitness);

    console.log("\n\n--- Transaction ---");
    console.log(JSON.stringify(transaction.export(), undefined, 2));

    console.log("\n\n--- Transaction hash---");
    console.log(transaction.hash)

    console.log("\n\n--- Transaction string ---")
    console.log(transaction.serialize(true));
    return transaction;
});

signTx
.then(transaction => {
    const client = new rpc.RPCClient(rpcNodeUrl);
    //send raw transaction
    return client.sendRawTransaction(transaction.serialize(true));
})
.then(res => {
    console.log("\n\n--- Response ---");
    console.log(res);
})
.catch(err => console.log(err));