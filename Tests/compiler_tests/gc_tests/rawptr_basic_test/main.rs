fn main() -> i32 {
    let b: Gc(i32) = Gc.New(42);
    let val: i32 = *b;
    val
}
