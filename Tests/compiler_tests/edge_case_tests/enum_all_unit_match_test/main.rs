enum Season {
    Spring,
    Summer,
    Autumn,
    Winter
}
fn score(s: Season) -> i32 {
    match s {
        Season::Spring => 1,
        Season::Summer => 2,
        Season::Autumn => 3,
        Season::Winter => 4
    }
}
fn main() -> i32 {
    score(Season::Spring) + score(Season::Summer) + score(Season::Autumn) + score(Season::Winter)
}
