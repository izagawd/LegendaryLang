fn main() -> i32 {
    let x = Option.Some(10);
    let y: Option(i32) = Option.None;
    let r1 = match x {
        Option.Some(v) => v,
        Option.None => 0
    };
    let r2 = match y {
        Option.Some(v) => v,
        Option.None => 0 - 1
    };
    r1 + r2
}
