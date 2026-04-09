struct Bro{val: i32}
struct Dd{ bro: Bro}
fn kk[T:! type](bruh: T) -> T{
    if (true){
        bruh
    } else{
         bruh
        }
   
}


fn main() -> i32{
    let gotten = make Dd {bro : make Bro {val : 5}};
    gotten.bro.val + 10
}

