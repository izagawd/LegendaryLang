fn main() -> i32 {
    let val: (i32 as NonExistentTrait).Output = 5;
    val
}
