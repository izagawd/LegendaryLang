fn main() -> i32 {
    let x = 10;
    {
        let r = &uniq x;
        *r = *r + 5;
    };
    x
}
