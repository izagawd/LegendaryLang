fn main() -> i32 {
    let x = 10;
    {
        let r = &mut x;
        *r = *r + 5;
    };
    x
}
