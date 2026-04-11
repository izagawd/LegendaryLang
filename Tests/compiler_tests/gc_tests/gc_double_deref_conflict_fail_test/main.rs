fn main() -> i32 {
    let b: GcMut(i32) = GcMut.New(42);
    let r: &mut i32 = &mut *b;
    let s: &i32 = &*b;
    *r + *s
}
