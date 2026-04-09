fn read_val(r: &i32) -> i32 {
    *r
}
fn main() -> i32 {
    let a = 42;
    read_val(&a)
}
