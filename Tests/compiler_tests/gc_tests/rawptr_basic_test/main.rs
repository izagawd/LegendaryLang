fn main() -> i32 {
    let b: GcMut(i32) = GcMut.New(42);
    let val: i32 = *b;
    val
}
