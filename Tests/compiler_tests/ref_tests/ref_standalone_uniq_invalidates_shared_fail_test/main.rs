fn main() -> i32 {
    let a = 5;
    let dd = &a;
    &mut a;
    *dd
}
