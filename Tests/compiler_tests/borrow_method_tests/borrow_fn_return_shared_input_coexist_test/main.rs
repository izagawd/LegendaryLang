fn bro(dd: &i32) -> &i32 {
    dd
}
fn main() -> i32 {
    let made = 21;
    let gotten = bro(&made);
    let another = &made;
    *gotten + *another
}
