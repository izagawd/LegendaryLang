fn read_val(r: &i32) -> i32 {
    *r
}
fn main() -> i32 {
    let a = 10;
    let r = &a;
    read_val(r) + read_val(r)
}
