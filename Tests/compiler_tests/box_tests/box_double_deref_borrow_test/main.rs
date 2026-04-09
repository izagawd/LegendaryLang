fn borrower(dd: &uniq Box(i32)) -> &uniq i32 {
    &uniq **dd
}

fn main() -> i32 {
    let dd: Box(i32) = Box.New(4);
    *borrower(&uniq dd)
}
