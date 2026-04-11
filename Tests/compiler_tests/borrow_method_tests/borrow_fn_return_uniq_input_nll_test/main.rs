fn bro(dd: &mut i32) -> &i32 {
    &*dd
}
fn main() -> i32 {
    let made = 5;
    let gotten = bro(&mut made);
    let val = *gotten;
    let another = &made;
    val + *another
}
