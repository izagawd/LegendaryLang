enum Choice {
    Yes(i32),
    No
}
fn make_choice(x: i32) -> Choice {
    Choice::Yes(x)
}
fn main() -> i32 {
    let c = make_choice(20);
    match c {
        Choice::Yes(v) => v,
        Choice::No => 0
    }
}
