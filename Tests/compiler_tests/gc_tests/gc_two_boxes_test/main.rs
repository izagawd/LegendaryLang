fn main() -> i32 {
    let a: Gc(i32) = Gc.New(10);
    let b: Gc(i32) = Gc.New(32);
    *a + *b
}
