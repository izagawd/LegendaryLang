fn take_box(b: Gc(i32)) -> i32 {
    *b
}

fn main() -> i32 {
    let b: Gc(i32) = Gc.New(77);
    take_box(b)
}
