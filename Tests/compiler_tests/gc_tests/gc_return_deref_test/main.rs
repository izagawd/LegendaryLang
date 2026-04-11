fn get_val(b: Gc(i32)) -> i32 {
    *b
}

fn main() -> i32 {
    let a: i32 = get_val(Gc.New(10));
    let b: i32 = get_val(Gc.New(32));
    a + b
}
